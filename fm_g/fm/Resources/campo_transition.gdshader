shader_type spatial;

uniform sampler2D current_tex;
uniform sampler2D next_tex;

uniform float transition : hint_range(0.0, 1.0);

void fragment() {
    vec4 col_current = texture(current_tex, UV);
    vec4 col_next = texture(next_tex, UV);

    vec4 white = vec4(1.0);

    vec4 result;

    if (transition < 0.5) {
        float t = transition * 2.0;
        result = mix(col_current, white, t);
    } else {
        float t = (transition - 0.5) * 2.0;
        result = mix(white, col_next, t);
    }

    ALBEDO = result.rgb;
}