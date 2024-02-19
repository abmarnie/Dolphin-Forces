using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    public static float ElapsedTimeS() => Time.GetTicksMsec() / 1000f;

    [Export] private Player _player = null!;
    [Export] private Node3D _underwaterTerrains = null!;
    [Export] private AudioStreamPlayer _music = null!;
    [Export] private AudioStreamPlayer _introFoghorn = null!;

    private AudioStream _musicMetal = ResourceLoader.Load<AudioStream>(
        "res://nathan/game_jam_metal Edit 1 Export 2.wav");
    private AudioStream _musicPeaceful = ResourceLoader.Load<AudioStream>(
        "res://nathan/Game Jam Edit 1 Export 1.wav");
    private float _musicPeacefulPlaybackPos;

    public override void _Ready() {
        _player.OnJump += SwitchToMetalMusic;
        _player.OnWaterEntry += SwitchToPeacefulMusic;

        _music.Stop();
        _introFoghorn.Play();

        void SwitchToPeacefulMusic() {
            _music.Stream = _musicPeaceful;
            _music.Play();
            _music.Seek(_musicPeacefulPlaybackPos);
        }

        void SwitchToMetalMusic() {
            _musicPeacefulPlaybackPos = _music.GetPlaybackPosition();
            _music.Stream = _musicMetal;
            _music.Play();
        }

    }

    public override void _Process(double delta) => _underwaterTerrains.Visible = Player.IsCameraUnderwater();

}