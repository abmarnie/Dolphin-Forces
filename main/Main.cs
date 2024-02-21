using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    public static float ElapsedTimeS() => Time.GetTicksMsec() / 1000f;

    [Export] Player _player = null!;
    [Export] Node3D _underwaterTerrains = null!;
    [Export] AudioStreamPlayer _music = null!;
    [Export] AudioStreamPlayer _introFoghorn = null!;

    AudioStream _musicMetal = GD.Load<AudioStream>(
        "res://nathan/game_jam_metal Edit 1 Export 2.wav");
    AudioStream _musicPeaceful = GD.Load<AudioStream>(
        "res://nathan/Game Jam Edit 1 Export 1.wav");
    float _musicPeacefulPlaybackPos;

    public override void _Ready() {
        _player.OnIntroEnd += PlayMetalMusic;
        _player.OnJump += PlayMetalMusic;
        _player.OnWaterEntry += PlayPeacefulMusic;

        _music.Stop();
        _introFoghorn.Play();

        void PlayPeacefulMusic() {
            _music.Stream = _musicPeaceful;
            _music.Play();
            _music.Seek(_musicPeacefulPlaybackPos);
        }

        void PlayMetalMusic() {
            _musicPeacefulPlaybackPos = _music.GetPlaybackPosition();
            _music.Stream = _musicMetal;
            _music.Play();
        }

        _underwaterTerrains.Visible = true;
    }

    public override void _Process(double delta) {
        if (!Player.HasIntroEnded()) {
            return;
        }

        _underwaterTerrains.Visible = _player.IsCameraUnderwater();
    }
}