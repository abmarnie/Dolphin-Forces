shader_type spatial;

uniform sampler2D albedo : source_color;

void vertex() {
	const float time_scale = 1.0;
	float time = (TIME * (0.5 + INSTANCE_CUSTOM.y) * time_scale) 
		+ (6.28318 * INSTANCE_CUSTOM.x);

	const float side_to_side = 1.0;
	VERTEX.x += cos(time) * side_to_side;
	
	const float pivot = 1.0;
	float pivot_angle = cos(time) * 0.1 * pivot;
	mat2 rot_mat = mat2(vec2(cos(pivot_angle), -sin(pivot_angle)),
		vec2(sin(pivot_angle), cos(pivot_angle)));

	const float fish_center = 0.0;
	const float fish_length = 2.0;
	float body = (VERTEX.z + fish_center) / fish_length;
	
	const float wave = 1.0;
	VERTEX.x += cos(time + body) * wave;
}
