using System;
using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    // For global guard clause to "pause" game.
    public static bool IsGameStarted() => _introScreenState == IntroScreenState.Ended;
    public static readonly Random Rng = new();

    [Export] Player _player = null!;
    [Export] Node3D _underwaterTerrains = null!;
    [Export] AudioStreamPlayer _introFoghorn = null!;

    // Intro.
    enum IntroScreenState { Starting, WaitingForPlayer, Ending, Ended }
    static IntroScreenState _introScreenState;
    [Export] ColorRect _introScreen = null!;
    [Export] Label _introText = null!;
    [Export] Label _continuePrompt = null!;

    public override void _Ready() {
        _introFoghorn.Play();
        _underwaterTerrains.Visible = false;

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

    }

    public override void _Input(InputEvent @event) {
        // End the intro once the player responds to the continue prompt.
        if (_introScreenState is IntroScreenState.WaitingForPlayer) {
            if (@event.IsActionReleased("attack")) {
                _introScreenState = IntroScreenState.Ending;
                var introOverlayFadeOut = _introScreen.CreateTween();
                _ = introOverlayFadeOut.TweenProperty(_introScreen, "modulate",
                    Colors.White with { A = 0 }, 3f);

                _player.PlaySfx();

                introOverlayFadeOut.Finished += () => {
                    _introScreenState = IntroScreenState.Ended;
                    _introScreen.Visible = false;

                    _player.GravityScale = 10f;
                    _player.PlayMetalMusic();
                };
            }
        }
    }

    public override void _Process(double delta) {
        if (!IsGameStarted()) {
            return;
        }

        _underwaterTerrains.Visible = _player.IsCameraUnderwater();
    }

}