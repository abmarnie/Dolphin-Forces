using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    public static float ElapsedTimeS => Time.GetTicksMsec() / 1000f;

    public static int NumDeadCamo;
    public static int NumDeadFlag;
    public static int NumDeadRussian;
    public static int NumDeadYellow;

    private AudioStreamPlayer _audioStreamPlayer = null!;
    private AudioStreamPlayer _fogHornPlayer = null!;
    private AudioStream _metal_music = null!;
    private AudioStream _peaceful_music = null!;
    private float _metal_playback;
    private float _peaceful_playback;

    private Node3D _terrain = null!;
    private Dolphin _player = null!;

    public override void _Ready() {
        _terrain = GetNode<Node3D>("%Terrain");
        _player = GetNode<Dolphin>("%Dolphin");
        _player.OnJump += SwitchToMetalMusic;
        _player.OnWaterEntry += SwitchToPeacefulMusic;
        _peaceful_music = ResourceLoader.Load<AudioStream>("res://nathan/Game Jam Edit 1 Export 1.wav");
        _metal_music = ResourceLoader.Load<AudioStream>("res://nathan/game_jam_metal Edit 1 Export 2.wav");
        _audioStreamPlayer = GetNode<AudioStreamPlayer>("%AudioStreamPlayer");
        _audioStreamPlayer.Stop();

        _fogHornPlayer = GetNode<AudioStreamPlayer>("%FogHornPlayer");
        Debug.Assert(_fogHornPlayer is not null);
        _fogHornPlayer.Play();

        Debug.Assert(_audioStreamPlayer is not null);
        Debug.Assert(_peaceful_music is not null);
        Debug.Assert(_metal_music is not null);

        // Dolphin.SetObjective(GetNode<ObjectiveManager>("%FlagBoats"));
        // _boats = this.Descendants<Boat>();
        // Debug.Assert(_boats is not null);
        // Debug.Assert(_boats.Count > 0);

    }

    private void SwitchToPeacefulMusic() {
        _metal_playback = _audioStreamPlayer.GetPlaybackPosition();
        _audioStreamPlayer.Stream = _peaceful_music;
        _audioStreamPlayer.Play();
        _audioStreamPlayer.Seek(_peaceful_playback);
    }

    private void SwitchToMetalMusic() {
        GD.Print("OnJump!");
        _peaceful_playback = _audioStreamPlayer.GetPlaybackPosition();
        _audioStreamPlayer.Stream = _metal_music;
        _audioStreamPlayer.Play();
        // _audioStreamPlayer.Seek(_metal_playback);
    }

    public override void _Process(double delta) => _terrain.Visible = Dolphin.IsCameraUnderwater;

    public static float Money => 1000f * NumDeadCamo + 500f * NumDeadFlag + 1250f * NumDeadRussian + 3000f * NumDeadYellow;

    public override void _PhysicsProcess(double delta) {
        Dolphin.MoneyLabel.Text = $"Money Earned: ${Money:N0}";
        if (Main.Money > Dolphin.SecondUpgradeCost) {
            Dolphin.MoneyLabel.Text = Dolphin.MoneyLabel.Text + "\n" + $"${Dolphin.SecondUpgradeCost} transfer queued. Max speed and fire rate increased.";

        } else if (Main.Money > Dolphin.FirstUpgradeCost) {
            Dolphin.MoneyLabel.Text = Dolphin.MoneyLabel.Text + "\n" + $"${Dolphin.FirstUpgradeCost} transfer queued. Torpedo fire rate increased.";
        }




    }

}