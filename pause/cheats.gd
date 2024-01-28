extends PanelContainer
class_name Cheats

# To be read and used in Food.gd, please don't overwrite.
static var cook_per_second_scale := 1.0

# To be read and used in MovementController.gd, please don't overwrite.
static var move_speed_scale := 1.0

signal new_scene_loaded 

@onready var _cook_speed_label: Label = %CookSpeedLabel
@onready var _move_speed_label: Label = %MoveSpeedLabel
@onready var _cook_speed_h_slider: HSlider = %CookSpeedHSlider
@onready var _move_speed_h_slider: HSlider = %MoveSpeedHSlider

func _ready() -> void:
	_cook_speed_h_slider.value = 1.0 / cook_per_second_scale
	_move_speed_h_slider.value = move_speed_scale
	
	_cook_speed_label.text = "Cook Speed: %d%%" \
		% int(_cook_speed_h_slider.value * 100)
	
	_move_speed_label.text = "Move Speed: %d%%" \
		% int(_move_speed_h_slider.value * 100)

func _on_cook_speed_h_slider_value_changed(new_val: float) -> void:
	assert(new_val != 0)
	cook_per_second_scale =  1.0 / new_val
	_cook_speed_label.text = "Cook Speed: %d%%" \
		% int(new_val * 100)

func _on_move_speed_h_slider_value_changed(new_val: float) -> void:
	assert(new_val != 0)
	move_speed_scale = new_val
	_move_speed_label.text = "Move Speed: %d%%" \
		% int(new_val * 100)


func _on_test_button_pressed():
	#new_scene_loaded.emit()
	#get_tree().change_scene_to_file("res://Levels/hub.tscn")
	pass

func _on_rooftop_button_pressed() -> void:
	pass
	#new_scene_loaded.emit()
	#get_tree().change_scene_to_file("res://Levels/Rooftop/Rooftop.tscn")

func _on_infinite_scroll_button_pressed():
	#new_scene_loaded.emit()
	#get_tree().change_scene_to_file("res://Levels/InfiniteScroll/InfiniteScroll.tscn")
	pass


func _on_everest_button_pressed():
	#new_scene_loaded.emit()
	#get_tree().change_scene_to_file("res://Levels/Everest/Everest.tscn")
	pass


func _on_docks_pressed():
	#new_scene_loaded.emit()
	#get_tree().change_scene_to_file("res://Levels/docks.tscn")
	pass
