using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    public static float ElapsedTimeS() => Time.GetTicksMsec() / 1000f;

    [Export] Player _player = null!;
    [Export] Node3D _underwaterTerrains = null!;
    [Export] AudioStreamPlayer _music = null!;
    [Export] AudioStreamPlayer _introFoghorn = null!;

    [Export] AudioStream _musicMetal = null!;
    [Export] AudioStream _musicPeaceful = null!;
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

        _underwaterTerrains.Visible = false;

    }

    public override void _Process(double delta) {
        if (!Player.HasIntroEnded()) {
            return;
        }

        _underwaterTerrains.Visible = _player.IsCameraUnderwater();
    }

}