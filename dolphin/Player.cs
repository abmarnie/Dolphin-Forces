using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Player : RigidBody3D {

    // Events are used to change music in main script.
    public event Action? OnJump;
    public event Action? OnWaterEntry;

    // Used to juice up the camera.
    [Export] Node3D _desiredCamPos = null!;
    [Export] Node3D _dolphins = null!;
    [Export] Camera3D _cam = null!;

    // Used for game progression.
    [Export] Label _moneyLabel = null!;
    float _money;

    // Dynamic visuals.
    [Export] AnimationTree _animTree = null!;
    Godot.Environment _waterEnv = GD.Load<Godot.Environment>(
        "res://water/underwater_environment.tres");

    public float MouseSens = 1;
    Vector3 _playerRotInput;
    Vector3 _camRotInput;
    float _dolphinPosXInput;

    const float DEFAULT_SPEED = 25f;
    float _maxSpeed = 50f;
    float _speed = DEFAULT_SPEED;

    [Export] Node3D _torpedoSpawn1 = null!;
    [Export] Node3D _torpedoSpawn2 = null!;
    PackedScene _torpedoFactory = GD.Load<PackedScene>("res://torpedos/torpedo.tscn");
    float _torpedoCooldown = 0.5f;
    int _torpedoFireCount; // For alternating between spawn1 and spawn2.
    float _torpedoFireTime;
    bool _isAttackInputPressed;

    [Export] AudioStreamPlayer3D _sfx = null!;
    AudioStream _splashSfx = GD.Load<AudioStream>("res://nathan/splash.mp3");
    AudioStream _robotSfx = GD.Load<AudioStream>("res://nathan/mixkit-futuristic-robot-movement-1412.wav");

    [Export] ColorRect _introOverlay = null!;
    [Export] Label _introMainLabel = null!;
    [Export] Label _introContinuePrompt = null!;
    const float INTRO_MAIN_TEXT_TIMELENGTH = 7f;
    bool _hasUserContinuedIntro;

    [Export] Control _pauseMenu = null!;

    const float LOOK_ANGLE_MAX = 5 * Mathf.Pi / 12; // 75 deg

    public override void _Input(InputEvent @event) {

        // End the intro once the player responds to the continue prompt.
        if (Main.ElapsedTimeS() > INTRO_MAIN_TEXT_TIMELENGTH) {
            if (@event.IsActionReleased("attack")) {
                if (!_hasUserContinuedIntro)
                    _sfx.Play();
                _hasUserContinuedIntro = true;
            }
        }

        if (IsIntroPlaying()) {
            return;
        }

        // Read inputs and cache the results for use in main update loops:

        if (@event is InputEventMouseMotion mouseMotion && IsUnderwater()) {
            const float sensScale = 0.005f;
            _playerRotInput.Y -= mouseMotion.Relative.X * MouseSens * sensScale;
            _playerRotInput.X -= mouseMotion.Relative.Y * MouseSens * sensScale;
            _playerRotInput.X = Mathf.Clamp(_playerRotInput.X, -LOOK_ANGLE_MAX, LOOK_ANGLE_MAX);

            const float xPosIncr = 0.1f;
            const float yRotIncr = 0.01f;
            const float zRotIncr = 0.1f;
            if (mouseMotion.Relative.X < 0) {
                _dolphinPosXInput += xPosIncr;
                _camRotInput.Y += yRotIncr;
                _camRotInput.Z += zRotIncr;

            } else if (mouseMotion.Relative.X > 0) {
                _dolphinPosXInput -= xPosIncr;
                _camRotInput.Y -= yRotIncr;
                _camRotInput.Z -= zRotIncr;
            }
        }

        if (@event.IsActionPressed("attack")) {
            _isAttackInputPressed = true;
        }

        if (@event.IsActionReleased("attack"))
            _isAttackInputPressed = false;

        const float speedScrollDelta = 1f;
        if (@event.IsActionReleased("scroll_up"))
            _speed += speedScrollDelta;
        else if (@event.IsActionReleased("scroll_down"))
            _speed -= speedScrollDelta;

        const float minSpeed = 5f;
        _speed = Mathf.Clamp(_speed, minSpeed, _maxSpeed);

    }

    public override void _Ready() {

        Debug.Assert(_waterEnv is not null);

        Input.MouseMode = Input.MouseModeEnum.Captured;

        Boat.OnKill += IncrementMoney;

        void IncrementMoney(float amount) {
            _money += amount;
            _moneyLabel.Text = $"Money Earned: ${_money:N0}";
        }

        _torpedoFireTime = -_torpedoCooldown;
        _introMainLabel.Modulate = Colors.White;
        _animTree.Set("parameters/speed_scale/scale", 1f);

        Debug.Assert(!_pauseMenu.Visible);
        Debug.Assert(!_introContinuePrompt.Visible);

    }

    bool _firstPhysicsTime = true;

    float _introlabelAlpha;
    static float _introMainTextTimer;
    float _introlColorRectAlpha = 1;
    const float CUTSCENE_END_TIME_LENGTH = 10f;

    float _continueLabelAlpha;

    bool _firstUpgradeObtained;
    bool _secondUpgradeObtained;

    int _numInfUpgrades;
    float _infiniteScalingUpgradeCost = 10000f;

    public override void _Process(double delta) {
        var camLerpWeight = IsUnderwater() ? 0.3f : 0.2f;

        _cam.GlobalPosition = _cam.GlobalPosition.Lerp(
            to: _desiredCamPos.GlobalPosition,
            weight: 0.3f
        );

        _cam.GlobalRotation = new Vector3(
            x: Mathf.LerpAngle(_cam.GlobalRotation.X, _desiredCamPos.GlobalRotation.X, camLerpWeight),
            y: Mathf.LerpAngle(_cam.GlobalRotation.Y, _desiredCamPos.GlobalRotation.Y, camLerpWeight),
            z: Mathf.LerpAngle(_cam.Rotation.Z, _desiredCamPos.Rotation.Z, camLerpWeight)
        );

        const float defaultFov = 75;
        const float fovScalingFactor = 0.25f;
        _cam.Fov = defaultFov + (defaultFov * (_speed - DEFAULT_SPEED) / DEFAULT_SPEED * fovScalingFactor);

        _dolphins.Rotation = _dolphins.Rotation with {
            Y = Mathf.LerpAngle(_dolphins.Rotation.Y, _camRotInput.Y, .1f),
            Z = Mathf.LerpAngle(_dolphins.Rotation.Z, _camRotInput.Z, .1f)
        };

        _dolphins.Position = _dolphins.Position with {
            X = Mathf.Lerp(_dolphins.Position.X, _dolphinPosXInput, 0.1f)
        };

        _camRotInput = _camRotInput.Lerp(Vector3.Zero, .1f);
        _dolphinPosXInput = Mathf.Lerp(_dolphinPosXInput, 0f, 0.1f);
    }

    public override void _PhysicsProcess(double delta) {

        const float alphaDelta = 0.0025f;
        if (!_hasUserContinuedIntro) {
            _introOverlay.Visible = true;
            _introlabelAlpha += alphaDelta;
            _introlabelAlpha = Mathf.Clamp(_introlabelAlpha, 0f, 1f);
            _introMainLabel.Modulate = new Color(1, 1, 1, _introlabelAlpha);
            if (Main.ElapsedTimeS() > INTRO_MAIN_TEXT_TIMELENGTH) {
                _continueLabelAlpha += 2f * alphaDelta;
                _introContinuePrompt.Visible = true;
                _continueLabelAlpha = Mathf.Clamp(_continueLabelAlpha, 0f, 1f);
                _introContinuePrompt.Modulate = new Color(1, 1, 1, _continueLabelAlpha);
            }
            return;
        } else if (_hasUserContinuedIntro && _introMainTextTimer < CUTSCENE_END_TIME_LENGTH) {
            _introMainTextTimer += (float)delta;
            _introlabelAlpha -= alphaDelta / 1.5f;
            _introlColorRectAlpha -= alphaDelta / 1.5f;
            _continueLabelAlpha -= alphaDelta / 1.5f;
            _introOverlay.Modulate = new Color(1, 1, 1, _introlColorRectAlpha);
            _introMainLabel.Modulate = new Color(1, 1, 1, _introlabelAlpha);
            _introContinuePrompt.Modulate = new Color(1, 1, 1, 1);
        } else {
            _firstPhysicsTime = false;
            _introOverlay.Visible = false;
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;

        var isTorpedoOffCooldown = Main.ElapsedTimeS() > _torpedoFireTime + _torpedoCooldown;
        if (isTorpedoOffCooldown && _isAttackInputPressed) {
            _torpedoFireCount++;
            var torpedo = _torpedoFactory.Instantiate<Torpedo>();
            GetTree().CurrentScene.AddChild(torpedo);
            torpedo.AddCollisionExceptionWith(this);
            if (_torpedoFireCount % 2 == 0) {
                torpedo.GlobalPosition = _torpedoSpawn1.GlobalPosition;
                torpedo.GlobalRotation = _torpedoSpawn1.GlobalRotation;
            } else {
                torpedo.GlobalPosition = _torpedoSpawn2.GlobalPosition;
                torpedo.GlobalRotation = _torpedoSpawn2.GlobalRotation;
            }
            var torpedoForce = 40f + _speed;
            torpedo.ApplyImpulse(-torpedoForce * torpedo.Basis.Z);
            _torpedoFireTime = Main.ElapsedTimeS();
        }

        const float firstUpgradeCost = 10000f;
        const float secondUpgradeCost = 20000f;


        if (_money >= firstUpgradeCost && !_firstUpgradeObtained) {
            // MoneyLabel.Text = MoneyLabel.Text + "\n" + "$2000 earned. Comrade unlocked.";
            _sfx.Stream = _robotSfx;
            _sfx.Play();

            var mainDolphin = GetNode<MeshInstance3D>("%CyborgDolphin_001");
            mainDolphin.Position = mainDolphin.Position with { X = -3.25f };
            _torpedoSpawn1.Position = _torpedoSpawn1.Position with { X = -3.25f };

            var brother = GetNode<MeshInstance3D>("%CyborgDolphin_002");
            brother.Visible = true;
            brother.Position = brother.Position with { X = 3.25f };
            _torpedoSpawn2.Position = _torpedoSpawn2.Position with { X = 3.25f };
            _firstUpgradeObtained = true;

            _torpedoCooldown /= 2f;

        } else if (_money >= secondUpgradeCost && !_secondUpgradeObtained) {
            _sfx.Stream = _robotSfx;
            _sfx.Play();

            var brother = GetNode<MeshInstance3D>("%CyborgDolphin");
            brother.Visible = true;
            brother.Position = brother.Position with { Y = 3.25f };
            _secondUpgradeObtained = true;
            _maxSpeed = 80;
            _torpedoCooldown /= 1.5f;
        } else if (_money >= ((_numInfUpgrades + 1) * _infiniteScalingUpgradeCost) + secondUpgradeCost
            && _secondUpgradeObtained) {

            _numInfUpgrades++;

            _sfx.Stream = _robotSfx;
            _sfx.Play();

            _maxSpeed *= 1.1f;
            _torpedoCooldown /= 1.1f;

            UpdatePlayerMoneyLabel();
        }


        _moneyLabel.Text = $"Money Earned: ${_money:N0}";

        if (_numInfUpgrades >= 1) {
            UpdatePlayerMoneyLabel();
        } else if (_money >= secondUpgradeCost) {
            _moneyLabel.Text = _moneyLabel.Text + "\n" + $"${secondUpgradeCost} transfer queued. Max speed (SCROLL_WHEEL) and fire rate increased.";
        } else if (_money >= firstUpgradeCost) {
            _moneyLabel.Text = _moneyLabel.Text + "\n" + $"${firstUpgradeCost} transfer queued. Torpedo fire rate increased.";
        }


        void UpdatePlayerMoneyLabel() => _moneyLabel.Text = _moneyLabel.Text
            + "\n" + $"${_infiniteScalingUpgradeCost} transfer queued. Max speed (SCROLL_WHEEL) and fire rate increased.";


        const float animSpeedTuningScale = 3f;
        _animTree.Set("parameters/speed_scale/scale", animSpeedTuningScale * _speed / DEFAULT_SPEED);

        if (IsUnderwater()) {
            Rotation = Rotation with {
                X = _playerRotInput.X,
                Y = _playerRotInput.Y
            };
        } else {
            _animTree.Set("parameters/speed_scale/scale", 0f);
            const float airborneRotSpeed = 2f;
            Rotation -= airborneRotSpeed * (float)delta * Vector3.Right;
            Rotation = Rotation with { X = Mathf.Clamp(Rotation.X, -LOOK_ANGLE_MAX, LOOK_ANGLE_MAX) };

            _playerRotInput = Rotation;
        }

        _cam.Environment = IsCameraUnderwater() ? _waterEnv : null;

    }

    bool _impulsedApplied;
    bool _firstTime = true;

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (IsIntroPlaying()) {
            return;
        }

        if (_firstTime) {
            OnJump?.Invoke();
        }


        if (IsUnderwater()) {
            if (_impulsedApplied) {
                _sfx.Stream = _splashSfx;
                _sfx.Play();
                OnWaterEntry?.Invoke();
            }
            state.LinearVelocity = -_speed * Basis.Z;
            GravityScale = 0.0f;
            _impulsedApplied = false;
        } else {
            if (!_impulsedApplied) {
                _sfx.Play();
                if (!_firstTime)
                    OnJump?.Invoke();
                ApplyImpulse(-2f * _speed * Basis.Z);
            }
            if (_firstTime)
                GravityScale = 1f;
            else
                GravityScale = 9.8f;
            _impulsedApplied = true;
        }
        _firstTime = false;

    }


    public static bool IsIntroPlaying() => _introMainTextTimer < CUTSCENE_END_TIME_LENGTH;

    // Used to update Terrain visbility.
    public bool IsCameraUnderwater() => _cam.GlobalPosition.Y <= -0.3f;

    bool IsUnderwater() => GlobalPosition.Y <= 0;


}