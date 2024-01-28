extends Node

# This is an autoload singleton. Access it by `LookInput.value`.

static var mouse_sens := 1.0
static var joystick_sens := 1.0 # TODO: Add settings slider.

# Please do not overwrite `value`.
var value := Vector2.ZERO:
	get:
		assert(mouse_sens > 0)
		assert(joystick_sens > 0)
		const joystick_strength = 15
		return value * (mouse_sens \
			if _last_input_event is InputEventMouse \
			else joystick_sens * joystick_strength)
	set(new_val):
		value = new_val

var _last_input_event: InputEvent
var _last_frame_gamepad_look_value: Vector2

func _input(event: InputEvent) -> void:
	_last_input_event = event
	if event is InputEventMouseMotion:
		value = event.relative
	
func _process(_delta: float) -> void:
	if _last_input_event is InputEventMouse:
		call_deferred("_reset_value") # So `value` reads 0 if no motion.
	else:
		_last_frame_gamepad_look_value = value
		#value = Input.get_vector("look_left", "look_right", \
			#"look_down", "look_up")
	
func _reset_value() -> void:
	value = Vector2.ZERO
