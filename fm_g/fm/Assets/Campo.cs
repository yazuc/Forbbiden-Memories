using Godot;

[Tool]
public partial class Campo : Node3D
{
	private MeshInstance3D _mesh_st_ally;
	private MeshInstance3D _mesh_st_enemy;
	private MeshInstance3D _mesh_mt_enemy;
	private MeshInstance3D _mesh_mt_ally;
	private MeshInstance3D _mesh_center;
	private StandardMaterial3D _material;

	private Texture2D[] _texturas;

	public override void _Ready()
	{
		_mesh_st_ally = GetNode<MeshInstance3D>("text_st_ally");
		_mesh_st_enemy = GetNode<MeshInstance3D>("text_st_enemy");
		_mesh_mt_enemy = GetNode<MeshInstance3D>("text_mt_ally");
		_mesh_mt_ally = GetNode<MeshInstance3D>("text_mt_enemy");
		_mesh_center = GetNode<MeshInstance3D>("text_center");

		// Pega o material embutido no GLB
		var original = (StandardMaterial3D)_mesh_st_ally.GetActiveMaterial(0);

		// DUPLICA para não alterar o asset global
		_material = (StandardMaterial3D)original.Duplicate();
		_mesh_st_ally.SetSurfaceOverrideMaterial(0, _material);
		_mesh_st_enemy.SetSurfaceOverrideMaterial(0, _material);
		_mesh_mt_enemy.SetSurfaceOverrideMaterial(0, _material);
		_mesh_mt_ally.SetSurfaceOverrideMaterial(0, _material);
		_mesh_center.SetSurfaceOverrideMaterial(0, _material);

		// Agora você pode usar PNGs externos normalmente
		_texturas = new Texture2D[]
		{
			GD.Load<Texture2D>("res://Assets/campos/campo_original/normalfield.png"),
			GD.Load<Texture2D>("res://Assets/campos/campo_agua/campo_agua_water_field.png"),
			GD.Load<Texture2D>("res://Assets/campos/campo_dark/dark_3.png"),
			GD.Load<Texture2D>("res://Assets/campos/campo_deserto/campo_deserto_wasteland.png"),
			GD.Load<Texture2D>("res://Assets/campos/campo_forest/campo_forest_campo_1.png"),
			GD.Load<Texture2D>("res://Assets/campos/campo_grass/campo_grama_grama.png"),
			GD.Load<Texture2D>("res://Assets/campos/campo_montanha/campo_montanha_montanha_3.png"),
		};

		SetEstadoCampo(2);
	}

	public void SetEstadoCampo(int estado)
	{
		if (estado < 0 || estado >= _texturas.Length)
			return;

		_material.AlbedoTexture = _texturas[estado];
	}
}
