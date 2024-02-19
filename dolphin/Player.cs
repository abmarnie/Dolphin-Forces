using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Player : RigidBody3D {

    public event Action? OnJump;
    public event Action? OnWaterEntry;

    [Export] private Camera3D _camera = null!;
    public bool IsCameraUnderwater() => _camera.GlobalPosition.Y <= -0.3f;

    [Export] private Label _moneyLabel = null!;
    public float Money { get; private set; }

    private bool IsUnderwater => GlobalPosition.Y <= 0;

    private AnimationTree _animTree = null!;

    private Godot.Environment _underwaterEnv = GD.Load<Godot.Environment>(
        "res://water/underwater_environment.tres");

    public float MouseSens = 1;
    private Vector3 _rotationFromMouse;

    private const float DEFAULT_SPEED = 25f;
    private float _speed = DEFAULT_SPEED;

    [Export] private Node3D _largeTorpedoSpawnLocation1 = null!;
    [Export] private Node3D _largeTorpedoSpawnLocation2 = null!;
    private PackedScene _largeTorpedoPackedScene = GD.Load<PackedScene>("res://torpedos/large_torpedo.tscn");
    private float _largeTorpedoCooldown = 0.5f;
    private float _lastLargeTorpedoFireTime;

    private AudioStream _splashSfx = GD.Load<AudioStream>("res://nathan/splash.mp3");
    private AudioStream _robotSfx = GD.Load<AudioStream>("res://nathan/mixkit-futuristic-robot-movement-1412.wav");

    private bool _isSplashSoundLoaded;

    [Export] private AudioStreamPlayer3D _sfx = null!;

    private const float INTRO_MAIN_TEXT_TIMELENGTH = 7f;
    public static bool IsIntroPlaying() => _introMainTextTimer < CUTSCENE_END_TIME_LENGTH;

    private float _cutsceneTimer;

    [Export] private ColorRect _introOverlay = null!;
    [Export] private Label _introMainLabel = null!;
    [Export] private Label _introContinueLabel = null!;

    private bool _hasPlayerPressedContinue;
    private bool _isAttackHeld;

    public override void _Input(InputEvent @event) {
        if (_cutsceneTimer > INTRO_MAIN_TEXT_TIMELENGTH) {
            if (@event.IsActionReleased("attack")) {
                if (!_hasPlayerPressedContinue)
                    _sfx.Play();
                _hasPlayerPressedContinue = true;
            }
        }

        if (IsIntroPlaying())
            return;

        if (@event is InputEventMouseMotion mouseMotion && IsUnderwater) {
            _rotationFromMouse.Y -= mouseMotion.Relative.X * MouseSens * 0.005f;
            _rotationFromMouse.X -= mouseMotion.Relative.Y * MouseSens * 0.005f;
            var lookAngleBounds = Mathf.DegToRad(75.0f);
            _rotationFromMouse.X = Mathf.Clamp(_rotationFromMouse.X, -lookAngleBounds, lookAngleBounds);
        }

        // if (@event.IsActionReleased("ui_cancel")) {
        //     Input.MouseMode = Input.MouseMode switch {
        //         Input.MouseModeEnum.Visible => Input.MouseModeEnum.Captured,
        //         Input.MouseModeEnum.Captured => Input.MouseModeEnum.Visible,
        //         _ => throw new NotImplementedException(),
        //     };
        // }

        if (@event.IsActionPressed("attack")) { // TODO: Cooldown bar async fill.
            _isAttackHeld = true;
        }

        if (@event.IsActionReleased("attack"))
            _isAttackHeld = false;


        const float speedScrollDelta = 1f;
        if (@event.IsActionReleased("scroll_up"))
            _speed += speedScrollDelta;
        else if (@event.IsActionReleased("scroll_down"))
            _speed -= speedScrollDelta;

        const float minSpeed = 5f;
        _speed = Mathf.Clamp(_speed, minSpeed, _maxSpeed);

    }

    private float _maxSpeed = 50f;


    private int _fireCount;

    public override void _Ready() {

        Boat.OnKill += (amount) => {
            Money += amount;
            _moneyLabel.Text = $"Money Earned: ${Money:N0}";
        };

        Input.MouseMode = Input.MouseModeEnum.Captured;

        _animTree.Set("parameters/speed_scale/scale", 1f);

        Debug.Assert(_underwaterEnv is not null);

        Debug.Assert(ContactMonitor);
        Debug.Assert(MaxContactsReported >= 1);
        BodyEntered += KillBoat;

        static void KillBoat(Node body) {
            if (body is Boat boat && boat.IsAlive) {
                boat.Kill();
            }
        }

        _lastLargeTorpedoFireTime = -_largeTorpedoCooldown;
        _introMainLabel.Modulate = new Color(1, 1, 1, 0);
        _introContinueLabel.Visible = false;

    }

    private bool _firstPhysicsTime = true;

    private float _introlabelAlpha;
    private static float _introMainTextTimer;
    private float _introlColorRectAlpha = 1;
    private const float CUTSCENE_END_TIME_LENGTH = 10f;

    private float _continueLabelAlpha;

    private bool _firstUpgradeObtained;
    private bool _secondUpgradeObtained;

    private float _firstUpgradeCost = 10000f;
    private float _secondUpgradeCost = 20000f;
    private int _numInfUpgrades;
    private float _infiniteScalingUpgradeCost = 10000f;


    public override void _PhysicsProcess(double delta) {

        _cutsceneTimer += (float)delta;
        const float alphaDelta = 0.0025f;
        if (!_hasPlayerPressedContinue) {
            _introOverlay.Visible = true;
            _introlabelAlpha += alphaDelta;
            _introlabelAlpha = Mathf.Clamp(_introlabelAlpha, 0f, 1f);
            _introMainLabel.Modulate = new Color(1, 1, 1, _introlabelAlpha);
            if (_cutsceneTimer > INTRO_MAIN_TEXT_TIMELENGTH) {
                _continueLabelAlpha += 2f * alphaDelta;
                _introContinueLabel.Visible = true;
                _continueLabelAlpha = Mathf.Clamp(_continueLabelAlpha, 0f, 1f);
                _introContinueLabel.Modulate = new Color(1, 1, 1, _continueLabelAlpha);
            }
            return;
        } else if (_hasPlayerPressedContinue && _introMainTextTimer < CUTSCENE_END_TIME_LENGTH) {
            _introMainTextTimer += (float)delta;
            _introlabelAlpha -= alphaDelta / 1.5f;
            _introlColorRectAlpha -= alphaDelta / 1.5f;
            _continueLabelAlpha -= alphaDelta / 1.5f;
            _introOverlay.Modulate = new Color(1, 1, 1, _introlColorRectAlpha);
            _introMainLabel.Modulate = new Color(1, 1, 1, _introlabelAlpha);
            _introContinueLabel.Modulate = new Color(1, 1, 1, 1);
        } else {
            _firstPhysicsTime = false;
            _introOverlay.Visible = false;
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;

        var isTorpedoOffCooldown = Main.ElapsedTimeS() > _lastLargeTorpedoFireTime + _largeTorpedoCooldown;
        if (isTorpedoOffCooldown && _isAttackHeld) {
            _fireCount++;
            var torpedo = _largeTorpedoPackedScene.Instantiate<LargeTorpedo>();
            GetTree().CurrentScene.AddChild(torpedo);
            torpedo.AddCollisionExceptionWith(this);
            if (_fireCount % 2 == 0) {
                torpedo.GlobalPosition = _largeTorpedoSpawnLocation1.GlobalPosition;
                torpedo.GlobalRotation = _largeTorpedoSpawnLocation1.GlobalRotation;
            } else {
                torpedo.GlobalPosition = _largeTorpedoSpawnLocation2.GlobalPosition;
                torpedo.GlobalRotation = _largeTorpedoSpawnLocation2.GlobalRotation;
            }
            var torpedoForce = 40f + _speed;
            torpedo.ApplyImpulse(-torpedoForce * torpedo.Basis.Z);
            _lastLargeTorpedoFireTime = Main.ElapsedTimeS();
        }


        if (Money >= _firstUpgradeCost && !_firstUpgradeObtained) {
            // MoneyLabel.Text = MoneyLabel.Text + "\n" + "$2000 earned. Comrade unlocked.";
            _sfx.Stream = _robotSfx;
            _sfx.Play();

            var mainDolphin = GetNode<MeshInstance3D>("%CyborgDolphin_001");
            mainDolphin.Position = mainDolphin.Position with { X = -3.25f };
            _largeTorpedoSpawnLocation1.Position = _largeTorpedoSpawnLocation1.Position with { X = -3.25f };

            var brother = GetNode<MeshInstance3D>("%CyborgDolphin_002");
            brother.Visible = true;
            brother.Position = brother.Position with { X = 3.25f };
            _largeTorpedoSpawnLocation2.Position = _largeTorpedoSpawnLocation2.Position with { X = 3.25f };
            _firstUpgradeObtained = true;

            _largeTorpedoCooldown /= 2f;

        } else if (Money >= _secondUpgradeCost && !_secondUpgradeObtained) {
            _sfx.Stream = _robotSfx;
            _sfx.Play();

            var brother = GetNode<MeshInstance3D>("%CyborgDolphin");
            brother.Visible = true;
            brother.Position = brother.Position with { Y = 3.25f };
            _secondUpgradeObtained = true;
            _maxSpeed = 80;
            _largeTorpedoCooldown /= 1.5f;
        } else if (Money >= ((_numInfUpgrades + 1) * _infiniteScalingUpgradeCost) + _secondUpgradeCost
            && _secondUpgradeObtained) {

            _numInfUpgrades++;

            _sfx.Stream = _robotSfx;
            _sfx.Play();

            _maxSpeed *= 1.1f;
            _largeTorpedoCooldown /= 1.1f;

            UpdatePlayerMoneyLabel();
        }


        _moneyLabel.Text = $"Money Earned: ${Money:N0}";

        if (_numInfUpgrades >= 1) {
            UpdatePlayerMoneyLabel();
        } else if (Money >= _secondUpgradeCost) {
            _moneyLabel.Text = _moneyLabel.Text + "\n" + $"${_secondUpgradeCost} transfer queued. Max speed (SCROLL_WHEEL) and fire rate increased.";
        } else if (Money >= _firstUpgradeCost) {
            _moneyLabel.Text = _moneyLabel.Text + "\n" + $"${_firstUpgradeCost} transfer queued. Torpedo fire rate increased.";
        }


        void UpdatePlayerMoneyLabel() => _moneyLabel.Text = _moneyLabel.Text
            + "\n" + $"${_infiniteScalingUpgradeCost} transfer queued. Max speed (SCROLL_WHEEL) and fire rate increased.";


        const float defaultFov = 75;
        const float fovScalingFactor = 0.25f; // Adjust this value as needed
        _camera.Fov = defaultFov + (defaultFov * (_speed - DEFAULT_SPEED) / DEFAULT_SPEED * fovScalingFactor);

        const float animSpeedTuningScale = 3f;
        _animTree.Set("parameters/speed_scale/scale", animSpeedTuningScale * _speed / DEFAULT_SPEED);

        if (IsUnderwater) {
            Rotation = new Vector3(_rotationFromMouse.X, _rotationFromMouse.Y, Rotation.Z);
        } else {
            const float airborneRotSpeed = 2f;
            Rotation -= airborneRotSpeed * (float)delta * Vector3.Right;

            var radians75 = Mathf.DegToRad(75.0f);
            Rotation = Rotation with { X = Mathf.Clamp(Rotation.X, -radians75, radians75) };
            // Rotation = new Vector3(Mathf.Clamp(Rotation.X, -radians75, radians75), Rotation.Y, Rotation.Y);

            _rotationFromMouse = Rotation;
            _animTree.Set("parameters/speed_scale/scale", 0f);
        }

        _camera.Environment = IsCameraUnderwater() ? _underwaterEnv : null;


    }

    private bool _impulsedApplied;
    private bool _firstTime = true;

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (IsIntroPlaying()) {
            return;
        }

        if (_firstTime) {
            OnJump?.Invoke();
        }


        if (IsUnderwater) {
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

}