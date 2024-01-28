using System.Diagnostics;
using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    public static float ElapsedTimeS => Time.GetTicksMsec() / 1000f;

    private AudioStreamPlayer _audioStreamPlayer = null!;
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
        Debug.Assert(_audioStreamPlayer is not null);
        Debug.Assert(_peaceful_music is not null);
        Debug.Assert(_metal_music is not null);

    }

    private void SwitchToPeacefulMusic() {
        _metal_playback = _audioStreamPlayer.GetPlaybackPosition();
        _audioStreamPlayer.Stream = _peaceful_music;
        _audioStreamPlayer.Seek(_peaceful_playback);
        _audioStreamPlayer.Play();
    }

    private void SwitchToMetalMusic() {
        _peaceful_playback = _audioStreamPlayer.GetPlaybackPosition();
        _audioStreamPlayer.Stream = _metal_music;
        _audioStreamPlayer.Seek(_metal_playback);
        _audioStreamPlayer.Play();
    }

    public override void _Process(double delta) => _terrain.Visible = Dolphin.IsCameraUnderwater;

}
