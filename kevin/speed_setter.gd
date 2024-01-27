extends Node3D

@onready var animation_tree = $AnimationTree
@export var speed_amplifier = 1.0
# Called when the node enters the scene tree for the first time.
func set_speed(speed):
	animation_tree.set("parameters/speed_scale/scale", speed * speed_amplifier)
