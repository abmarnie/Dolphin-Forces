using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Dolphin : RigidBody3D {

    public static bool IsCameraUnderwater => _camera.GlobalPosition.Y <= -0.3f;
    public static Label MoneyLabel;
    private static Camera3D _camera = null!;

    public event Action? OnInfUpgrade;
    public event Action? OnJump;
    public event Action? OnWaterEntry;
    public bool IsUnderwater => GlobalPosition.Y <= 0;

    private AnimationTree _animTree = null!;

    private Godot.Environment _underwaterEnv = null!;
    public float _mouseSens = 1;
    private Vector3 _rotationFromMouse;

    private const float DEFAULT_SPEED = 25f;
    private float _speed = DEFAULT_SPEED;

    private float _largeTorpedoCooldown = 0.5f;
    private PackedScene _largeTorpedoPackedScene = GD.Load<PackedScene>("res://torpedos/large_torpedo.tscn");
    private Node3D _largeTorpedoSpawnLocation1 = null!;
    private Node3D _largeTorpedoSpawnLocation2 = null!;
    private float _lastLargeTorpedoFireTime;

    private AudioStream _splash_sfx = ResourceLoader.Load<AudioStream>("res://nathan/splash.mp3");
    private AudioStream _robot_sfx = ResourceLoader.Load<AudioStream>("res://nathan/mixkit-futuristic-robot-movement-1412.wav");


    private bool _isSplashSoundLoaded;

    private AudioStreamPlayer3D _audioStreamPlayer = null!;

    public static Boat NearestObjective = null!;
    private static Sprite3D _objectiveArrow = null!;

    private const float _cutsceneTimeLength = 7f;
    public static bool CutscenePlaying => _cutsceneEndTimer < _cutsceneEndTimeLength;

    // private const float _cutsceneTimeLength = 10f;
    // public static bool CutscenePlaying => _cutsceneTimer < _cutsceneTimeLength;

    private static float _cutsceneTimer = 0f;

    private static ColorRect _introColorRect = null!;
    private static Label _introLabel = null!;
    private static Label _continueLabel = null!;

    private static bool _playerRespondedToCutsceneFinish = false;
    private bool _boostQueued;

    private static bool _isAttackHeld = false;


    public override void _Input(InputEvent @event) {
        if (_cutsceneTimer > _cutsceneTimeLength) {
            if (@event.IsActionReleased("attack")) {
                if (!_playerRespondedToCutsceneFinish)
                    _audioStreamPlayer.Play();
                _playerRespondedToCutsceneFinish = true;
            }
        }

        if (CutscenePlaying)
            return;

        if (@event is InputEventMouseMotion mouseMotion && IsUnderwater) {
            _rotationFromMouse.Y -= mouseMotion.Relative.X * _mouseSens * 0.005f;
            _rotationFromMouse.X -= mouseMotion.Relative.Y * _mouseSens * 0.005f;
            var lookAngleBounds = Mathf.DegToRad(75.0f);
            _rotationFromMouse.X = Mathf.Clamp(_rotationFromMouse.X, -lookAngleBounds, lookAngleBounds);
        }

        if (IsUnderwater && _secondUpgradeObtained) {
            // if (@event.IsActionPressed("boost")) {
            //     _boostQueued = true;
            // }
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
        _speed = Mathf.Clamp(_speed, minSpeed, maxSpeed);

    }

    private float maxSpeed = 50f;


    private int fireCount = 0;

    public override void _Ready() {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _camera = GetNode<Camera3D>("%Camera3D");

        _animTree = GetNode<AnimationTree>("%AnimationTree");
        Debug.Assert(_animTree is not null);
        _animTree.Set("parameters/speed_scale/scale", 1f);

        _underwaterEnv = (Godot.Environment)GD.Load("res://water/underwater_environment.tres");
        Debug.Assert(_underwaterEnv is not null);

        Debug.Assert(ContactMonitor);
        Debug.Assert(MaxContactsReported >= 1);
        BodyEntered += KillBoat;

        static void KillBoat(Node body) {
            if (body is Boat boat && !boat.IsDead) {
                boat.IsDead = true;
            }
        }

        _lastLargeTorpedoFireTime = -_largeTorpedoCooldown;
        _largeTorpedoSpawnLocation1 = GetNode<Node3D>("%LargeTorpedoSpawnLocation1");
        Debug.Assert(_largeTorpedoSpawnLocation1 is not null);

        _largeTorpedoSpawnLocation2 = GetNode<Node3D>("%LargeTorpedoSpawnLocation2");
        Debug.Assert(_largeTorpedoSpawnLocation2 is not null);


        _audioStreamPlayer = GetNode<AudioStreamPlayer3D>("%AudioStreamPlayer3D");
        // _audioStreamPlayer.Finished += LoadSplashSound;

        _objectiveArrow = this.GetDescendant<Sprite3D>()!;
        Debug.Assert(_objectiveArrow is not null);
        _objectiveArrow.Visible = false;

        MoneyLabel = this.GetDescendant<Label>()!;
        Debug.Assert(MoneyLabel is not null);

        _introColorRect = this.GetDescendant<ColorRect>()!;
        Debug.Assert(_introColorRect is not null);

        _introLabel = _introColorRect.GetChild<Label>(0);
        Debug.Assert(_introLabel is not null);
        _introLabel.Modulate = new Color(1, 1, 1, 0);

        _continueLabel = _introColorRect.GetChild<Label>(1);
        Debug.Assert(_continueLabel is not null);
        _continueLabel.Visible = false;

    }

    private bool _firstPhysicsTime = true;

    private float _introlabelAlpha = 0;
    private static float _cutsceneEndTimer = 0;
    private float _introlColorRectAlpha = 1;
    private const float _cutsceneEndTimeLength = 10f;

    private float _continueLabelAlpha = 0;

    private bool _firstUpgradeObtained = false;
    private bool _secondUpgradeObtained = false;

    public static float FirstUpgradeCost = 10000f;
    public static float SecondUpgradeCost = 20000f;
    public static int numInfUpgrades = 0;
    public static float InfiniteScalingUpgradeCost = 10000f;


    public override void _PhysicsProcess(double delta) {

        _cutsceneTimer += (float)delta;
        const float alphaDelta = 0.0025f;
        // _cutsceneTimer < _cutsceneTimeLength ||
        if (!_playerRespondedToCutsceneFinish) {
            _introColorRect.Visible = true;
            _introlabelAlpha += alphaDelta;
            _introlabelAlpha = Mathf.Clamp(_introlabelAlpha, 0f, 1f);
            _introLabel.Modulate = new Color(1, 1, 1, _introlabelAlpha);
            if (_cutsceneTimer > _cutsceneTimeLength) {
                _continueLabelAlpha += 2f * alphaDelta;
                _continueLabel.Visible = true;
                _continueLabelAlpha = Mathf.Clamp(_continueLabelAlpha, 0f, 1f);
                _continueLabel.Modulate = new Color(1, 1, 1, _continueLabelAlpha);
            }
            return;
        } else if (_playerRespondedToCutsceneFinish && _cutsceneEndTimer < _cutsceneEndTimeLength) {
            _cutsceneEndTimer += (float)delta;
            _introlabelAlpha -= alphaDelta / 1.5f;
            _introlColorRectAlpha -= alphaDelta / 1.5f;
            _continueLabelAlpha -= alphaDelta / 1.5f;
            _introColorRect.Modulate = new Color(1, 1, 1, _introlColorRectAlpha);
            _introLabel.Modulate = new Color(1, 1, 1, _introlabelAlpha);
            _continueLabel.Modulate = new Color(1, 1, 1, 1);
        } else {
            _firstPhysicsTime = false;
            _introColorRect.Visible = false;
        }

        Input.MouseMode = Input.MouseModeEnum.Captured;

        var isTorpedoOffCooldown = Main.ElapsedTimeS > _lastLargeTorpedoFireTime + _largeTorpedoCooldown;
        if (isTorpedoOffCooldown && _isAttackHeld) {
            fireCount++;
            var torpedo = _largeTorpedoPackedScene.Instantiate<LargeTorpedo>();
            GetTree().CurrentScene.AddChild(torpedo);
            torpedo.AddCollisionExceptionWith(this);
            if (fireCount % 2 == 0) {
                torpedo.GlobalPosition = _largeTorpedoSpawnLocation1.GlobalPosition;
                torpedo.GlobalRotation = _largeTorpedoSpawnLocation1.GlobalRotation;
            } else {
                torpedo.GlobalPosition = _largeTorpedoSpawnLocation2.GlobalPosition;
                torpedo.GlobalRotation = _largeTorpedoSpawnLocation2.GlobalRotation;
            }
            var torpedoForce = 40f + _speed;
            torpedo.ApplyImpulse(-torpedoForce * torpedo.Basis.Z);
            _lastLargeTorpedoFireTime = Main.ElapsedTimeS;
        }


        if (Main.Money >= FirstUpgradeCost && !_firstUpgradeObtained) {
            // MoneyLabel.Text = MoneyLabel.Text + "\n" + "$2000 earned. Comrade unlocked.";
            _audioStreamPlayer.Stream = _robot_sfx;
            _audioStreamPlayer.Play();

            var mainDolphin = GetNode<MeshInstance3D>("%CyborgDolphin_001");
            mainDolphin.Position = mainDolphin.Position with { X = -3.25f };
            _largeTorpedoSpawnLocation1.Position = _largeTorpedoSpawnLocation1.Position with { X = -3.25f };

            var brother = GetNode<MeshInstance3D>("%CyborgDolphin_002");
            brother.Visible = true;
            brother.Position = brother.Position with { X = 3.25f };
            _largeTorpedoSpawnLocation2.Position = _largeTorpedoSpawnLocation2.Position with { X = 3.25f };
            _firstUpgradeObtained = true;

            _largeTorpedoCooldown /= 2f;

        } else if (Main.Money >= SecondUpgradeCost && !_secondUpgradeObtained) {
            _audioStreamPlayer.Stream = _robot_sfx;
            _audioStreamPlayer.Play();

            var brother = GetNode<MeshInstance3D>("%CyborgDolphin");
            brother.Visible = true;
            brother.Position = brother.Position with { Y = 3.25f };
            _secondUpgradeObtained = true;
            maxSpeed = 80;
            _largeTorpedoCooldown /= 1.5f;
        } else if (Main.Money >= (numInfUpgrades + 1) * InfiniteScalingUpgradeCost + SecondUpgradeCost
            && _secondUpgradeObtained) {

            numInfUpgrades++;

            _audioStreamPlayer.Stream = _robot_sfx;
            _audioStreamPlayer.Play();

            maxSpeed *= 1.1f;
            _largeTorpedoCooldown /= 1.1f;

            OnInfUpgrade?.Invoke();
        }



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

        _camera.Environment = IsCameraUnderwater ? _underwaterEnv : null;


    }

    private bool _impulsedApplied;
    private bool _firstTime = true;

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (CutscenePlaying) {
            return;
        }

        if (_firstTime) {
            OnJump?.Invoke();
        }


        if (IsUnderwater) {
            if (_impulsedApplied) {
                _audioStreamPlayer.Stream = _splash_sfx;
                _audioStreamPlayer.Play();
                OnWaterEntry?.Invoke();
            }
            // if (_firstTime)
            //     _audioStreamPlayer.Play();
            // if (_boostQueued) {
            //     // _boostQueued = false;
            //     // ApplyImpulse(-100f * Basis.Z);
            // } else
            state.LinearVelocity = -_speed * Basis.Z;
            GravityScale = 0.0f;
            _impulsedApplied = false;
        } else {
            if (!_impulsedApplied) {
                _audioStreamPlayer.Play();
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