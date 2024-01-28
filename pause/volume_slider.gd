extends HSlider

# So that the slider values persist across scene loads (using match _bus_name).
static var _master_volume_linear := 1.0
static var _ost_volume_linear := 1.0
static var _sfx_volume_linear := 1.0

const _err_message := "volume_slider.gd's _bus_name field is wrong"

var _bus_index: int
@export var _bus_name: String:
	get:
		return _bus_name
	set(value):
		_bus_name = value
		_bus_index = AudioServer.get_bus_index(_bus_name)

func _ready() -> void:
	value_changed.connect(_on_value_changed)
	match _bus_name:
		"Master": value = _master_volume_linear
		"OST": value = _ost_volume_linear
		"SFX": value = _sfx_volume_linear
		_: print(_err_message)
	
func _on_value_changed(new_val: float) -> void:
	match _bus_name: 
		"Master": _master_volume_linear = new_val
		"OST": _ost_volume_linear = new_val
		"SFX": _sfx_volume_linear = new_val
		_: print(_err_message)
	AudioServer.set_bus_volume_db(
		_bus_index,
		linear_to_db(new_val)
	)
	
