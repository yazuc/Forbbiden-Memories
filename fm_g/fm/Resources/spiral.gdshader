shader_type canvas_item;

// Godot 4 replacement for SCREEN_TEXTURE
uniform sampler2D screen_texture : hint_screen_texture;
uniform sampler2D NOISE_TEXTURE;
uniform sampler2D spiral_texture;


// Controls
uniform float intensity : hint_range(0.0, 10.0) = 1.5;
uniform float speed : hint_range(0.0, 10.0) = 3.0;
uniform float frequency : hint_range(1.0, 10.0) = 3.0;

uniform vec2 rel_rect_size = vec2(1024.0, 600.0);

// =========================
// Spiral Function
// =========================
float draw_spiral(vec2 uv, float rotation){
	float r = length(uv) + 0.0001; // prevent log(0)
	float phi = atan(uv.y, uv.x) + rotation;

	float spiral_phi = (log(r) * frequency - phi) / TAU;
	float spiral_r = exp((TAU * floor(spiral_phi) + phi) / frequency);

	return spiral_r;
}

// =========================
// Rotation helper
// =========================
vec2 rotate(vec2 p, float a){
	return vec2(
		cos(a)*p.x - sin(a)*p.y,
		sin(a)*p.x + cos(a)*p.y
	);
}

// =========================
// Fragment
// =========================
void fragment() {
	vec2 uv = UV - 0.5;

	// Fix aspect ratio
	uv.x *= rel_rect_size.x / rel_rect_size.y;

	float dist = length(uv);

	// =========================
	// Noise (chaos)
	// =========================
	float noise = texture(NOISE_TEXTURE, uv * 2.5 + TIME * 0.2).r;
	float noise2 = texture(NOISE_TEXTURE, uv * 4.0 - TIME * 0.15).r;

	// Distort UV slightly
	uv += (noise - 0.5) * 0.15 * intensity;

	// =========================
	// Spiral layers
	// =========================
	float rot = TIME * speed;

	float spiral1 = draw_spiral(uv, rot + noise);
	float spiral2 = draw_spiral(uv * 1.2, -rot * 0.7 + noise2);

	float combined = mix(spiral1, spiral2, 0.5);

	// =========================
	// Swirl distortion (screen)
	// =========================
	float angle = intensity * (0.1 - dist) * 6.28318;

	vec2 warped = rotate(uv, angle + noise);

	vec2 disp = (warped - uv) * rel_rect_size * SCREEN_PIXEL_SIZE;
	disp.y = -disp.y;

	vec2 screen_uv = SCREEN_UV;
	screen_uv.y = 1.0 - screen_uv.y;

	vec4 screen_col = texture(screen_texture, screen_uv + disp);

	// =========================
	// Color energy (fusion glow)
	// =========================
	// Intensity (0 → 1)
	float flow = spiral1 * 12.0 - TIME * 1.0;
	float pull = 1.0 - dist;
	float e = fract(flow);
	e *= pull;

	// Stronger contrast in gradient
	e = pow(e, 3.0);

	vec3 blue = vec3(0.1, 0.6, 1.0);
	vec3 white = vec3(0.1, 0.6, 1.0);

	// Proper blend (now actually reaches white)
	vec3 energy = mix(blue, white, e);

	// Apply spiral
	energy *= spiral2;


	// =========================
	// Combine
	// =========================
	vec3 final_col = energy * 5.5;

	
	// =========================
	// Circular mask (portal)
	// =========================
	float mask = smoothstep(0.6, 0.1, dist);
	COLOR = vec4(final_col * mask, mask);
}
