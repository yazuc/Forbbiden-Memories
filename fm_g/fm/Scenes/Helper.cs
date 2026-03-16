using Godot;
using System;

namespace fm
{
	public partial class Helper : Node
	{
		[Export] public PackedScene carta3D {get;set;}
		[Export] public TextureRect LifePoint{get;set;}
		public List<Node3D> _cartasInstanciadas {get;set;}
		public Texture2D texture {get;set;}
		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			texture = GD.Load<Texture2D>("res://Assets/COM_active.png");
		}

		// Called every frame. 'delta' is the elapsed time since the previous frame.
		public override void _Process(double delta)
		{
		}

		public void Flipa(string ID)
		{
			var cartaInstanciada = PegaNodoCarta3d(ID);
			if(cartaInstanciada.IsFaceDown){
				cartaInstanciada.SetFaceDown(!cartaInstanciada.IsFaceDown);				
			}
		}

		public void RemoveDasInstanciadas(Node3D carta)
		{
			if(_cartasInstanciadas.Contains(carta))
			{
				_cartasInstanciadas.Remove(carta);
			}
		}
		
		public Carta3d PegaNodoCarta3d(string ID)
		{
			GD.Print("Procurando carta3d com ID: " + ID);
			GD.Print("Cartas instanciadas: " + _cartasInstanciadas.Count);
			return _cartasInstanciadas.OfType<Carta3d>().FirstOrDefault(x => x.markerName == ID);
		}	

		public int PegaSlotByMarker(string marker){
			var nodes = GetTree().GetNodesInGroup("cartas");						

			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					if(marker == meuNode.markerName){
						return meuNode.slotPlaced;
					}
				}
			}
			//nao achou
			return -1;
		}

		public bool PodeBate(List<Marker3D> SlotsCampoIni){
			var nodes = GetTree().GetNodesInGroup("cartas");
			if(nodes.Count() == 0) return false;

			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					foreach(var inimigo in SlotsCampoIni){
						if(inimigo.Name == meuNode.markerName){
							return false;
						}						
					}
				}
				else
				{
					GD.Print("item não é carta3d");
				}
			}			
			return true;
		}

		public bool PegaNodoNoSlot(Marker3D slotDestino){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					if(IsInstanceValid(meuNode))
					{
						if(slotDestino.Name == meuNode.markerName){
							return true;
						}						
					}
				}
			}			
			return false;
		}		

		public Node3D InstanciaNodo(Marker3D slotDestino, bool criarNovo = true){
			var nodes = GetTree().GetNodesInGroup("cartas");
			foreach(var item in nodes){
				if(item is Carta3d meuNode){
					if(slotDestino.Name == meuNode.markerName){
						return meuNode;
					}
				}
			}
			
			if(!criarNovo)
				return null;
			
			//nao achou
			var novaCarta3d = carta3D.Instantiate<Node3D>();
			novaCarta3d.AddToGroup("cartas");
			GetTree().CurrentScene.AddChild(novaCarta3d);
			novaCarta3d.GlobalPosition = slotDestino.GlobalPosition;
			novaCarta3d.GlobalRotation = slotDestino.GlobalRotation;
			_cartasInstanciadas.Add(novaCarta3d);
			
			return novaCarta3d;
		}

		public async Task TransitionTo(Camera3D targetCam, double duration, Camera3D _transitionCam, bool STOP)
		{			
			Viewport viewport = GetViewport();
			Camera3D currentCam = viewport.GetCamera3D();			
			if (currentCam == null || currentCam == targetCam) return;        
			STOP = true;
			_transitionCam.GlobalTransform = currentCam.GlobalTransform;
			_transitionCam.Fov = currentCam.Fov;
			_transitionCam.MakeCurrent();
			
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.SetEase(Tween.EaseType.InOut);

			tween.TweenProperty(_transitionCam, "global_transform", targetCam.GlobalTransform, duration);
			tween.TweenProperty(_transitionCam, "fov", targetCam.Fov, duration);
			await ToSignal(tween, Tween.SignalName.Finished);

			targetCam.MakeCurrent();
			STOP = false;
		}	

		public void PrintInstancia(Node3D instancia)
		{
			if(instancia is Carta3d cartaInstanciada)
			{
				GD.Print($"carta {cartaInstanciada.carta.ToString()} - fieldzone {cartaInstanciada.markerName.ToString()} - posicao {cartaInstanciada.Defesa.ToString()}");
			}
		}

		public void PrintTodasInstancias(){
			foreach(var item in _cartasInstanciadas)
				PrintInstancia(item);
		}

		public List<(string, bool)> DevolvePosicoes()
		{
			var tuple = new List<(string carta,bool defesa)>();	
			foreach(var item in _cartasInstanciadas)
			{
				
				if(IsInstanceValid(item) && item is Carta3d nodo)
				{
					GD.Print("DEVOLVE POS:" + nodo.markerName + " - " + nodo.Defesa);
					tuple.Add((nodo.markerName, nodo.Defesa));
				}	
			}
			
			return tuple;
		}	

		public void SwitchTurn(MaoJogador mao)
		{
			var textureLocal = LifePoint.Texture;						
			LifePoint.Texture = texture;			
			texture = textureLocal;				
		}
	}	
}
