extends Button

@onready var fry = %Fry

func _ready():
	connect("mouse_entered", play)

func play():
	fry.play()
