shader_type spatial;
render_mode unshaded, cull_disabled;

// O Godot passa a textura do Viewport para cá
uniform sampler2D albedo_texture : source_color, filter_nearest; 

uniform float progress : hint_range(0.0, 1.1) = 0.0; // 0 = Visível, 1 = Invisível

void fragment() {
    // IMPORTANTE: ViewportTextures às vezes precisam ter o UV.y invertido
    // vec2 v_uv = vec2(UV.x, 1.0 - UV.y); 
    vec2 v_uv = UV;
    
    vec4 card_tex = texture(albedo_texture, v_uv);
    
    // Invertendo a lógica para que 0.0 seja a base da carta no SubViewport
    float current_y = 1.0 - v_uv.y;
    
    // Descarte da Carta
    if (current_y < progress) {
        discard;
    }
    
    ALBEDO = card_tex.rgb;
    ALPHA = card_tex.a;
}