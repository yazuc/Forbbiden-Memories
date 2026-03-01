shader_type spatial;
render_mode unshaded, cull_disabled;

uniform sampler2D albedo_texture : source_color, filter_nearest;
uniform sampler2D noise_texture; 

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float edge_width : hint_range(0.0, 0.5) = 0.1;
uniform vec4 fire_color : source_color = vec4(5.0, 1.5, 0.0, 1.0); // HDR mais forte

void fragment() {
    vec2 v_uv = UV;
    vec4 card_tex = texture(albedo_texture, v_uv);
    float noise = texture(noise_texture, v_uv).r;
    
    float current_y = 1.0 - v_uv.y;
    
    // AJUSTE 1: Remapeamos o progresso para que ele cubra o ruído e a altura
    // Isso garante que no 1.0, o valor de 'threshold' seja maior que qualquer pixel possível
    float threshold = progress * 1.5; 
    
    // AJUSTE 2: A máscara agora leva em conta o ruído de forma mais agressiva
    float mask = (current_y + noise * 0.3) - threshold;
    
    // Se mask for menor que 0, o pixel some
    if (mask < 0.0) {
        discard;
    }
    
    // AJUSTE 3: Borda de fogo que suaviza conforme a carta some
    // O clamp evita que a borda "estoure" visualmente no final
    float edge = smoothstep(0.0, edge_width, mask);
    edge = clamp(edge, 0.0, 1.0);
    
    // Mistura a textura da carta com a cor do fogo
    // Quando edge é 0 (perto do descarte), fica fire_color
    vec3 final_color = mix(fire_color.rgb, card_tex.rgb, edge);
    
    ALBEDO = final_color;
    ALPHA = card_tex.a;
}