using Godot;
using System;
namespace fm
{	
	[Tool]
	public partial class FreeDuelCellHd : TextureRect
	{
		private int colunasMax = 5;
		private int linhasMax = 8;
		private int tamanhoFrame = 107;
		[Export] public float escalaDesejada = 2.5f; 
		private float inicioX = 554.222f; 
		private float inicioY = 59.238f;

		// Agora controlamos apenas um índice global
		private int indexAtual = 0;
		private int totalFrames = 40; // 0 até 38
		
		public void _ready()
		{
			//this.CustomMinimumSize = new Vector2(tamanhoFrame * escalaDesejada, tamanhoFrame * escalaDesejada);
			this.CustomMinimumSize = new Vector2(tamanhoFrame * escalaDesejada, tamanhoFrame * escalaDesejada);
			// Configure como a imagem deve se comportar dentro desse novo tamanho
			this.ExpandMode = ExpandModeEnum.IgnoreSize; // Permite que o TextureRect ignore o tamanho da textura original
			this.StretchMode = StretchModeEnum.Scale;    // Estica a região do Atlas para preencher o CustomMinimumSize	
		}

		public override void _Process(double delta)
		{
			// Exemplo: Mover um por um com as setas
			//if (Input.IsActionJustPressed("ui_right")) IrParaIndex(indexAtual + 1);
			//if (Input.IsActionJustPressed("ui_left"))  IrParaIndex(indexAtual - 1);
		}

		public void IrParaIndex(int novoIndex)
		{
			if (Texture is not AtlasTexture atlasTexture) return;
				
			if (Texture.ResourceLocalToScene == false) 
			{
				atlasTexture = (AtlasTexture)Texture.Duplicate();
				Texture = atlasTexture;
			}

			// Garante que o index fique entre 0 e 38
			indexAtual = Mathf.Clamp(novoIndex, 0, totalFrames - 1);

			// CONVERSÃO LINEAR PARA GRADE:
			int coluna = indexAtual % colunasMax;
			int linha = indexAtual / colunasMax;

			float novoX = inicioX + (coluna * tamanhoFrame);
			float novoY = inicioY + (linha * tamanhoFrame);

			Rect2 region = atlasTexture.Region;
			region.Position = new Vector2(novoX, novoY);
			atlasTexture.Region = region;

			GD.Print($"Index: {indexAtual} | Grade: [{coluna},{linha}] | Pos: {region.Position}");
		}
}
}
