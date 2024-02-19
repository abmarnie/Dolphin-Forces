using Godot;

namespace DolphinForces;

public partial class Main : Node3D {

    public static float ElapsedTimeS() => Time.GetTicksMsec() / 1000f;
    public static float Money { get; private set; }

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
        _player.OnInfUpgrade += UpdatePlayerMoneyLabel;
        Boat.OnKill += IncrementMoney;

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

        static void IncrementMoney(float amount) => Money += amount;
    }

    public override void _Process(double delta) => _underwaterTerrains.Visible = Player.IsCameraUnderwater;

    public override void _PhysicsProcess(double delta) {
        Player.MoneyLabel.Text = $"Money Earned: ${Money:N0}";
        if (Player.numInfUpgrades >= 1) {
            UpdatePlayerMoneyLabel();
        } else if (Money >= Player.SecondUpgradeCost) {
            Player.MoneyLabel.Text = Player.MoneyLabel.Text + "\n" + $"${Player.SecondUpgradeCost} transfer queued. Max speed (SCROLL_WHEEL) and fire rate increased.";

        } else if (Money >= Player.FirstUpgradeCost) {
            Player.MoneyLabel.Text = Player.MoneyLabel.Text + "\n" + $"${Player.FirstUpgradeCost} transfer queued. Torpedo fire rate increased.";
        }

    }

    // TODO: Put this in Dolphin/Player.cs.
    private void UpdatePlayerMoneyLabel() => Player.MoneyLabel.Text = Player.MoneyLabel.Text
        + "\n" + $"${Player.InfiniteScalingUpgradeCost} transfer queued. Max speed (SCROLL_WHEEL) and fire rate increased.";
}