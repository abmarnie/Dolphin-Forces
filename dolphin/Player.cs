using System;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Player : RigidBody3D {

    // For global guard clause to "pause" game.
    public static bool HasIntroEnded() => _introScreenState == IntroScreenState.Ended;

    // For updating Terrain visbility in main script.
    public bool IsCameraUnderwater() => _cam.GlobalPosition.Y <= -0.3f;

    // Events for changing music in main script.
    public event Action? OnIntroEnd;
    public event Action? OnJump;
    public event Action? OnWaterEntry;

    // For smoother camera and dolphin rotation with respect to player input.
    [Export] Node3D _desiredCamPos = null!;
    [Export] Node3D _dolphinsNode = null!;
    [Export] Camera3D _cam = null!;

    // For game progression.
    [Export] Label _moneyLabel = null!;
    float _money;

    // Dynamic visuals.
    [Export] AnimationTree _animTree = null!;
    [Export] MeshInstance3D[] _dolphinMeshes = null!;
    Godot.Environment _underwaterRenderEnv = GD.Load<Godot.Environment>(
        "res://water/underwater_environment.tres");

    // Movement.
    const float LOOK_ANGLE_MAX = 5 * Mathf.Pi / 12; // 75 deg
    public float MouseSens = 1;
    Vector3 _nextRotation;
    Vector3 _dolphinRotSmoothing;
    float _dolphinPosXInput;
    const float DEFAULT_SPEED = 25f;
    float _maxSpeed = 50f;
    float _speed = DEFAULT_SPEED;
    bool IsUnderwater => GlobalPosition.Y <= 0;
    bool _wasAirborne = true;

    // Torpedo logic.
    [Export] Node3D _torpedoSpawn1 = null!;
    [Export] Node3D _torpedoSpawn2 = null!;
    PackedScene _torpedoFactory = GD.Load<PackedScene>("res://torpedos/torpedo.tscn");
    float _attackCooldown = 0.5f;
    int _numTorpedosFired; // For alternating between spawn1 and spawn2.
    float _lastAttackTime;
    bool _isAttackInputPressed;

    // Sfx.
    [Export] AudioStreamPlayer3D _sfx = null!;
    AudioStream _splashSfx = GD.Load<AudioStream>("res://nathan/splash.mp3");
    AudioStream _robotSfx = GD.Load<AudioStream>(
        "res://nathan/mixkit-futuristic-robot-movement-1412.wav");

    // Intro.
    enum IntroScreenState { Starting, WaitingForPlayer, Ending, Ended }
    static IntroScreenState _introScreenState;
    [Export] ColorRect _introScreen = null!;
    [Export] Label _introText = null!;
    [Export] Label _continuePrompt = null!;

    // Pause menu.
    [Export] Control _pauseMenu = null!;

    // Game progression.
    int _numUpgrades;
    const float UPGRADE_COST = 10000f;

    public override void _Input(InputEvent @event) {
        // End the intro once the player responds to the continue prompt.
        if (_introScreenState is IntroScreenState.WaitingForPlayer) {
            if (@event.IsActionReleased("attack")) {
                _introScreenState = IntroScreenState.Ending;
                _sfx.Play();
                var introOverlayFadeOut = _introScreen.CreateTween();
                _ = introOverlayFadeOut.TweenProperty(_introScreen, "modulate",
                    Colors.White with { A = 0 }, 3f);
                introOverlayFadeOut.Finished += () => {
                    _introScreenState = IntroScreenState.Ended;
                    OnIntroEnd?.Invoke();
                    GravityScale = 10f;
                    _introScreen.Visible = false;
                };
            }
        }

        if (!HasIntroEnded()) {
            return;
        }

        // Rest of this method caches inputs for use in update loops.

        if (@event is InputEventMouseMotion mouseMotion && IsUnderwater) {
            // For rotating the player root (inside `_PhysicsProcess`).
            const float sensScale = 0.005f;
            _nextRotation.Y -= mouseMotion.Relative.X * MouseSens * sensScale;
            _nextRotation.X -= mouseMotion.Relative.Y * MouseSens * sensScale;
            _nextRotation.X = Mathf.Clamp(_nextRotation.X, -LOOK_ANGLE_MAX, LOOK_ANGLE_MAX);

            // For juicing up the camera and player offset (inside `_Process`).
            const float xPosIncr = 0.1f;
            const float yRotIncr = 0.01f;
            const float zRotIncr = 0.1f;
            if (mouseMotion.Relative.X < 0) {
                _dolphinPosXInput += xPosIncr;
                _dolphinRotSmoothing.Y += yRotIncr;
                _dolphinRotSmoothing.Z += zRotIncr;

            } else if (mouseMotion.Relative.X > 0) {
                _dolphinPosXInput -= xPosIncr;
                _dolphinRotSmoothing.Y -= yRotIncr;
                _dolphinRotSmoothing.Z -= zRotIncr;
            }
        }

        if (@event.IsActionPressed("attack")) {
            _isAttackInputPressed = true;
        }

        if (@event.IsActionReleased("attack")) {
            _isAttackInputPressed = false;
        }

        const float speedScrollDelta = 1f;
        if (@event.IsActionReleased("scroll_up")) {
            _speed += speedScrollDelta;
        } else if (@event.IsActionReleased("scroll_down")) {
            _speed -= speedScrollDelta;
        }

        const float minSpeed = 5f;
        _speed = Mathf.Clamp(_speed, minSpeed, _maxSpeed);

    }

    public override void _Ready() {
        Debug.Assert(_underwaterRenderEnv is not null);
        Debug.Assert(!_pauseMenu.Visible);

        Input.MouseMode = Input.MouseModeEnum.Captured;

        _lastAttackTime = -_attackCooldown; // Allow player to fire right away.
        _animTree.Set("parameters/speed_scale/scale", 1f);

        Rotation = Rotation with { X = -LOOK_ANGLE_MAX };

        // Intro labels begin invisible, then they fade in.
        _introText.Modulate = Colors.White with { A = 0 };
        _continuePrompt.Modulate = Colors.White with { A = 0 };

        _introScreenState = IntroScreenState.Starting;
        var introTextFadeIn = _introText.CreateTween();
        _ = introTextFadeIn.TweenProperty(_introText, "modulate", Colors.White, 1f); // 16f;
        introTextFadeIn.Finished += FadeInContinuePrompt;

        void FadeInContinuePrompt() {
            _continuePrompt.Visible = true;
            var continuePromptFadeIn = _continuePrompt.CreateTween();
            _ = continuePromptFadeIn.TweenProperty(_continuePrompt, "modulate", Colors.White, 1f);

            // Player is stuck on intro screen until they respond.
            continuePromptFadeIn.Finished += () => _introScreenState = IntroScreenState.WaitingForPlayer;
        }

        Boat.OnKill += IncrementMoney;

        void IncrementMoney(float amount) {
            _money += amount;
            _moneyLabel.Text = $"Money Earned: ${_money:N0}"
                + (
                    !_moneyLabel.Text.Contains('\n') ? ""
                    : _moneyLabel.Text[_moneyLabel.Text.IndexOf('\n')..]
                );

            var hasEnoughForNextUpgrade = _money >= UPGRADE_COST * (_numUpgrades + 1);
            if (hasEnoughForNextUpgrade) {
                if (_numUpgrades == 0) {
                    PerformUpgrade(
                        newMaxSpeed: _maxSpeed,
                        newAttackCooldown: _attackCooldown / 2f
                    );

                    var startingDolphin = _dolphinMeshes[1];
                    startingDolphin.Position = startingDolphin.Position with { X = -3.25f };
                    _torpedoSpawn1.Position = _torpedoSpawn1.Position with { X = -3.25f };

                    var secondDolphin = _dolphinMeshes[2];
                    secondDolphin.Visible = true;
                    secondDolphin.Position = secondDolphin.Position with { X = 3.25f };
                    _torpedoSpawn2.Position = _torpedoSpawn2.Position with { X = 3.25f };

                } else if (_numUpgrades == 1) {
                    PerformUpgrade(
                        newMaxSpeed: 80f,
                        newAttackCooldown: _attackCooldown / 1.5f
                    );

                    var thirdDolphin = _dolphinMeshes[0];
                    thirdDolphin.Visible = true;
                    thirdDolphin.Position = thirdDolphin.Position with { Y = 3.25f };
                } else if (_numUpgrades > 1) {
                    PerformUpgrade(
                        newMaxSpeed: _maxSpeed * 1.1f,
                        newAttackCooldown: _attackCooldown / 1.1f
                    );
                }

                void PerformUpgrade(float newMaxSpeed, float newAttackCooldown) {
                    _numUpgrades++;
                    _sfx.Stream = _robotSfx;
                    _sfx.Play();
                    _moneyLabel.Text += $"\n${UPGRADE_COST} transfer queued."
                        + $" Max speed (SCROLL_WHEEL) and fire rate increased.";
                    _maxSpeed = newMaxSpeed;
                    _attackCooldown = newAttackCooldown;
                }
            }
        }
    }

    public override void _Process(double delta) {
        if (!HasIntroEnded()) {
            return;
        }

        var camLerpWeight = IsUnderwater ? 0.3f : 0.2f;

        _cam.GlobalPosition = _cam.GlobalPosition.Lerp(
            _desiredCamPos.GlobalPosition, camLerpWeight);

        _cam.GlobalRotation = new Vector3(
            x: Mathf.LerpAngle(_cam.GlobalRotation.X, _desiredCamPos.GlobalRotation.X, camLerpWeight),
            y: Mathf.LerpAngle(_cam.GlobalRotation.Y, _desiredCamPos.GlobalRotation.Y, camLerpWeight),
            z: Mathf.LerpAngle(_cam.Rotation.Z, _desiredCamPos.Rotation.Z, camLerpWeight)
        );

        // Camera FoV scales relative to current speed for funny.
        const float defaultFov = 75;
        const float fovScalingFactor = 0.25f;
        _cam.Fov = defaultFov + (defaultFov * (_speed - DEFAULT_SPEED) / DEFAULT_SPEED * fovScalingFactor);
        _cam.Fov = Mathf.Clamp(_cam.Fov, 1f, 179f);
    }

    public override void _PhysicsProcess(double delta) {
        if (!HasIntroEnded()) {
            return;
        }

        // Underwater visual effects.
        _cam.Environment = IsCameraUnderwater() ? _underwaterRenderEnv : null;

        // Pause anim if airborne. Otherwise make it scale with speed. 
        const float animSpeedTuningScale = 3f;
        _animTree.Set("parameters/speed_scale/scale",
            IsUnderwater ? animSpeedTuningScale * _speed / DEFAULT_SPEED
            : 0f);

        var isAttackOffCooldown = Main.ElapsedTimeS() > _lastAttackTime + _attackCooldown;
        if (_isAttackInputPressed && isAttackOffCooldown) {
            // Spawn torpedo and prevent self-collisions.
            var torpedo = _torpedoFactory.Instantiate<Torpedo>();
            GetTree().CurrentScene.AddChild(torpedo);
            torpedo.AddCollisionExceptionWith(this);

            // Alternate between left and right. These coincide if player doesn't
            // have first upgrade. 
            _numTorpedosFired++;
            if (_numTorpedosFired % 2 == 0) {
                torpedo.GlobalPosition = _torpedoSpawn1.GlobalPosition;
                torpedo.GlobalRotation = _torpedoSpawn1.GlobalRotation;
            } else {
                torpedo.GlobalPosition = _torpedoSpawn2.GlobalPosition;
                torpedo.GlobalRotation = _torpedoSpawn2.GlobalRotation;
            }

            // Send torpedo forwards.
            var force = 40f + _speed;
            torpedo.ApplyImpulse(-force * torpedo.Basis.Z);

            // For checking if attack is off cooldown.
            _lastAttackTime = Main.ElapsedTimeS();
        }

        if (IsUnderwater) {
            // Rotate from player inputs.
            Rotation = _nextRotation;

            // The rest is for visual flourish.

            _dolphinsNode.Rotation = _dolphinsNode.Rotation with {
                Y = Mathf.LerpAngle(_dolphinsNode.Rotation.Y, _dolphinRotSmoothing.Y, .1f),
                Z = Mathf.LerpAngle(_dolphinsNode.Rotation.Z, _dolphinRotSmoothing.Z, .1f)
            };

            _dolphinsNode.Position = _dolphinsNode.Position with {
                X = Mathf.Lerp(_dolphinsNode.Position.X, _dolphinPosXInput, 0.1f)
            };

            _dolphinPosXInput = Mathf.Lerp(_dolphinPosXInput, 0f, 0.1f);
            _dolphinRotSmoothing = _dolphinRotSmoothing.Lerp(Vector3.Zero, .1f);
        } else {
            // Simulate "dolphin jump" rotation.
            const float airborneRotSpeed = 2f;
            _nextRotation -= airborneRotSpeed * (float)delta * Vector3.Right;
            _nextRotation = _nextRotation with { X = Mathf.Clamp(_nextRotation.X, -LOOK_ANGLE_MAX, LOOK_ANGLE_MAX) };

            Rotation = _nextRotation;
        }
    }

    bool _firstTimeCallingIntegrateForces; // To prevent erroneous sfx on game start. 

    public override void _IntegrateForces(PhysicsDirectBodyState3D state) {
        if (!HasIntroEnded()) {
            return;
        }

        if (IsUnderwater) {
            // Player only moves forward.
            state.LinearVelocity = -_speed * Basis.Z;

            if (_wasAirborne) {
                // The player just entered the water. 
                GravityScale = 0.0f;
                OnWaterEntry?.Invoke();
                _sfx.Stream = _splashSfx;
                _sfx.Play();
            }

            _wasAirborne = false;
        } else {

            if (!_wasAirborne) {
                // Unless they just left the water, in which case, simulate a jump.
                ApplyImpulse(-2f * _speed * Basis.Z);
                GravityScale = 10f;
                OnJump?.Invoke();

                // Since player starts airborne, this check is needed to prevent
                // erroneous splash sfx right after intro ends.
                if (!_firstTimeCallingIntegrateForces) {
                    _sfx.Stream = _splashSfx;
                    _sfx.Play();
                }
            }

            _wasAirborne = true;
        }

        _firstTimeCallingIntegrateForces = false;
    }

}