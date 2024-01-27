extends Node3D

var children_to_move = []
var base_speed = 5.0
@export var bounds = Vector3(5, 0, 5)
@export var min_speed = 0.2
@export var max_speed = 0.7
@export var rotation_speed = .05
@export var speed_change_frequency = 1.0
var targets = {}
var markers = {}
var sine_params = {}

func _ready():
	children_to_move = get_children()
	for child in children_to_move:
		if child is Node3D:
			child.scale *= randf_range(.2, 2)
			child.top_level = true
			targets[child] = get_random_target_point()
			var marker = Marker3D.new()  # Replace Marker3D with the actual marker type
			child.add_child(marker)
			marker.top_level = true
			markers[child] = marker
			sine_params[child] = {'amplitude': randf_range(min_speed, max_speed), 'frequency': randf_range(.02, .6) * speed_change_frequency, 'phase': randf_range(0, 10)}  # Initialize sine wave parameters

# Function to get a random target point within the bounds
func get_random_target_point():
	return Vector3(randf_range(-bounds.x, bounds.x), randf_range(-bounds.y, bounds.y), randf_range(-bounds.z, bounds.z)) + global_position

# Called every frame. 'delta' is the elapsed time since the previous frame.
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	for child in children_to_move:
		if child is Node3D:
			var params = sine_params[child]
			params['phase'] += delta * params['frequency']
			# Adjust sine wave to vary speed between min_speed and max_speed
			var sine_value = (sin(params['phase']) + 1) / 2  # Normalize between 0 and 1
			var current_speed = min_speed + sine_value * (max_speed - min_speed)
			if child.has_method("set_speed"):
				child.set_speed(current_speed)
			# Move forward with varying speed
			child.position += -child.basis.z * current_speed * delta

			# Gradually rotate to face the target
			var marker = markers[child]
			marker.global_position = child.global_position
			marker.look_at(targets[child], Vector3.UP)
			child.rotation.y = lerp_angle(child.rotation.y, marker.rotation.y, rotation_speed)
			child.rotation.x = lerp_angle(child.rotation.x, marker.rotation.x, rotation_speed)

			# Check if the fish has reached or is close to its target
			var distance_to_target = child.global_position.distance_to(targets[child])
			var distance_from_global_position = global_position.distance_to(child.global_position)
			if distance_to_target < 2.0 or is_out_of_global_bounds(distance_from_global_position):
				targets[child] = get_random_target_point()

# Function to check if a distance is out of the global bounds
func is_out_of_global_bounds(distance):
	# Calculate the maximum allowed distance based on the bounds
	var max_distance = max(bounds.x, bounds.y, bounds.z) * 2
	return distance > max_distance
