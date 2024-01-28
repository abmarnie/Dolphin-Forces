extends CanvasLayer

@onready var fry: AudioStreamPlayer = $Fry
@onready var color_rect: ColorRect = $ColorRect
@onready var settings_panel: PanelContainer = $ColorRect/SettingsPanelContainer
@onready var main_container: VBoxContainer = $ColorRect/MainVboxContainer
@onready var fullscreen_toggle: CheckButton = $ColorRect/SettingsPanelContainer/VBoxContainer/HBoxContainer/FullscreenCheckButton
@onready var mouse_sens_label: Label = $ColorRect/SettingsPanelContainer/VBoxContainer/MouseSensLabel
@onready var mouse_sens_h_slider: HSlider = $ColorRect/SettingsPanelContainer/VBoxContainer/MouseSensHSlider
@onready var cheats_panel_container: Cheats = %CheatsPanelContainer
@onready var player = get_parent().get_parent()
var paused := false

func _ready():
	if paused:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	else:
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
	mouse_sens_h_slider.value = LookInput.mouse_sens
	visible = false
	settings_panel.visible = false
	cheats_panel_container.connect("new_scene_loaded", pause)

func _input(event: InputEvent):
	if event.is_action_released("pause"):
		if settings_panel.visible:
			settings_panel.visible = false
		else:
			pause()
	#if event.is_action_just_pressed("pause"):
		#if player.current_state != player.STATES.NORMAL and !player.on_main_menu:
			#player.end_conversation()
		#else:

	
	if !visible: 
		return
	
	#if OS.is_debug_build() and event.is_action_released("toggle_cheats_menu"):
		#cheats_panel_container.visible = !cheats_panel_container.visible
	

func _process(_delta: float):
	if settings_panel.visible:
		mouse_sens_label.text = "Mouse Sensitivity: %d%%" \
			% int(mouse_sens_h_slider.value * 100)
			
	main_container.visible = !settings_panel.visible

func _on_quit_pressed():
	get_tree().quit()

func _on_resume_pressed():
	pause()

func _on_settings_pressed():
	settings_panel.visible = true

func _on_settings_back_pressed():
	settings_panel.visible = false

func _on_settings_fullscreen_check_button_toggled(toggled_on: bool):
	if toggled_on:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)

func _on_mouse_sens_h_slider_value_changed(value: float):
	LookInput.mouse_sens = value
	
func pause():
	visible = !get_tree().paused
	get_tree().paused = !get_tree().paused
	paused = !paused
	if paused:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	else:
		Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
