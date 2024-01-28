extends Camera3D

@onready var camera_3d = %Camera3D
@onready var dolphins = %Dolphins
var desired_z_rotation = 0.0
var desired_y_rotation = 0.0
var desired_x_position = 0.0
var turn_force = .1
var position_force = .3

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	self.set_environment(camera_3d.environment)
	if get_parent().IsUnderwater:
		turn_force = .3
	else:
		turn_force = .2
	self.global_position = lerp(self.global_position, camera_3d.global_position, position_force)
	self.global_rotation.x = lerp_angle(self.global_rotation.x, camera_3d.global_rotation.x, turn_force)
	self.global_rotation.y = lerp_angle(self.global_rotation.y, camera_3d.global_rotation.y, turn_force)
	self.global_rotation.z = lerp_angle(self.rotation.z, camera_3d.rotation.z, turn_force)
	self.fov = lerpf(self.fov, camera_3d.fov, .1)
	dolphins.rotation.z = lerp_angle(dolphins.rotation.z, desired_z_rotation, .1)
	dolphins.rotation.y = lerp_angle(dolphins.rotation.y, desired_y_rotation, .1)
	dolphins.position.x = lerpf(dolphins.position.x, desired_x_position, .1)
	desired_z_rotation = lerpf(desired_z_rotation, 0, .1)
	desired_y_rotation = lerpf(desired_y_rotation, 0, .1)
	desired_x_position = lerpf(desired_x_position, 0, .1)
	
func _input(event):
	if event is InputEventMouseMotion and get_parent().IsUnderwater:
		if event.relative.x < 0:
			desired_z_rotation += .1
			desired_y_rotation += .01
			desired_x_position += .1
		elif event.relative.x > 0:
			desired_z_rotation -= .1
			desired_y_rotation -= .01
			desired_x_position -= .1
