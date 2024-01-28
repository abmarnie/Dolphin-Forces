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

        // Dolphin.SetObjective(GetNode<ObjectiveManager>("%FlagBoats"));
        // _boats = this.Descendants<Boat>();
        // Debug.Assert(_boats is not null);
        // Debug.Assert(_boats.Count > 0);

    }

    private void SwitchToPeacefulMusic() {
        _metal_playback = _audioStreamPlayer.GetPlaybackPosition();
        _audioStreamPlayer.Stream = _peaceful_music;
        _audioStreamPlayer.Seek(_peaceful_playback);
        _audioStreamPlayer.Play();
    }

    private void SwitchToMetalMusic() {
        GD.Print("OnJump!");
        _peaceful_playback = _audioStreamPlayer.GetPlaybackPosition();
        _audioStreamPlayer.Stream = _metal_music;
        _audioStreamPlayer.Seek(_metal_playback);
        _audioStreamPlayer.Play();
    }

    public override void _Process(double delta) => _terrain.Visible = Dolphin.IsCameraUnderwater;

    public override void _PhysicsProcess(double delta) {
        var money = 1000f * NumDeadCamo + 500f * NumDeadFlag + 1250f * NumDeadRussian + 3000f * NumDeadYellow;
        Dolphin.MoneyLabel.Text = $"Money Earned: ${money:N0}";
    }

    // public override void _PhysicsProcess(double delta) => Dolphin.NearestObjective = GetClosestBoat();

    // private Boat? GetClosestBoat() {
    //     if (_boats == null || _boats.Count == 0) {
    //         GD.Print("out of boats");
    //         return null;
    //     }

    //     Boat? closestBoat = null;
    //     var minDistance = float.MaxValue;
    //     var playerPositionXZ = new Vector3(_player.GlobalPosition.X, 0, _player.GlobalPosition.Z);

    //     foreach (var boat in _boats) {
    //         // Skip this boat if it's dead
    //         if (boat.IsDead) {
    //             continue;
    //         }

    //         var boatPositionXZ = new Vector3(boat.GlobalPosition.X, 0, boat.GlobalPosition.Z);
    //         var distance = playerPositionXZ.DistanceTo(boatPositionXZ);

    //         if (distance < minDistance) {
    //             minDistance = distance;
    //             closestBoat = boat;
    //         }
    //     }

    //     return closestBoat;
    // }



}
