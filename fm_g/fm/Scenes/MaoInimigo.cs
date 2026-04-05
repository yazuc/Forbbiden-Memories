using Godot;
using QuickType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static fm.Function;

namespace fm 
{
    public partial class MaoInimigo : Node2D 
    {
        [Export] public Node2D IndicadorTriangulo;
        [Export] public Node3D Seletor3D; // O cursor que aparece no campo
        
        private int _indiceVisualMao = 0;
        private int _indiceVisualCampo = 0;
        [Export] public Camera3D CameraHand;
		[Export] public Camera3D CameraField;
		[Export] public Camera3D CameraInimigo;
        public Camera3D _transitionCam;
        
        // Referências externas
        [Export] public Mao MaoControlIA { get; set; } 
        public AnimationP _anim;
        public Helper Tools;
        public GameLoop gameLoop { get; set; }        

        // Listas de slots (recebidas do GameLoop)
        public Godot.Collections.Array<Marker3D> SlotsCampoIni = new();
        public Godot.Collections.Array<Marker3D> SlotsCampoSTIni = new();
         public Godot.Collections.Array<Marker3D> SlotsCampo = new();
        public Godot.Collections.Array<Marker3D> SlotsCampoST = new();

        public override void _Ready()
        {
            _anim = GetNode<AnimationP>("../AnimationP");
            Seletor3D = GetNode<Node3D>("../SeletorInimigo");
            Tools = GetNode<Helper>("../Helper");        
            _transitionCam = new Camera3D();
			AddChild(_transitionCam);
            
            if (Seletor3D != null) Seletor3D.Visible = false;
            if (IndicadorTriangulo != null) IndicadorTriangulo.Visible = false;
        }

        /// <summary>
        /// Move o cursor visualmente até o destino desejado.
        /// </summary>
        public async Task<string> ExecutarMovimentoVisual(List<int> indiceAlvo, bool noCampo = false, bool isSpellTrap = false, Cards carta = null, bool facedown = false)
        {
            if (!noCampo)
            {
                IndicadorTriangulo.Visible = true;
                foreach (var indice in indiceAlvo)
                {
                    while (_indiceVisualMao != indice)
                    {
                        _indiceVisualMao += (_indiceVisualMao < indice) ? 1 : -1;
                        await AtualizarPosicaoIndicadorInimigo();
                        await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
                    }        
                }
            }
            else
            {
                Seletor3D.Visible = true;
                var slotsAlvo = isSpellTrap ? SlotsCampoSTIni : SlotsCampoIni;
                bool slotvazio = false; 
                while (!slotvazio)
                {                    
                    var slotDestino = Tools.PegaSlotByMarker(slotsAlvo[_indiceVisualCampo].Name);
                    await AtualizarPosicaoSeletor3DInimigo(slotsAlvo);
                    if(slotDestino == -1)
                    {
                        await Tools.TransitionTo(CameraHand, 0.5f, _transitionCam, false);
                        await Instancia3D(slotsAlvo[_indiceVisualCampo], carta, facedown);
                        return slotsAlvo[_indiceVisualCampo].Name;
                    }
                    _indiceVisualCampo += 1;
                    await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
                }
            }

            return "";
        }

        private async Task AtualizarPosicaoIndicadorInimigo()
        {
            var carta = MaoControlIA.GetCarta(_indiceVisualMao);
            if (IsInstanceValid(carta))
            {
                // Invertido para o topo da tela
                Vector2 targetPos = carta.GlobalPosition + new Vector2(-10, 180); 
                Tween tween = GetTree().CreateTween();
                tween.TweenProperty(IndicadorTriangulo, "position", targetPos, 0.01f)
                     .SetTrans(Tween.TransitionType.Quad)
                     .SetEase(Tween.EaseType.Out);
                await ToSignal(tween, Tween.SignalName.Finished);
            }
        }

        public async Task AtualizarPosicaoSeletor3DInimigo(Godot.Collections.Array<Marker3D> slots, string Indice = "")
        {
            if (Seletor3D == null || slots.Count == 0) return;

            var slotDestino = slots[_indiceVisualCampo];
            if (!string.IsNullOrWhiteSpace(Indice))
            {
                Seletor3D.Visible = true;
                var slot = Tools.PegaSlotByMarker(Indice);
                slotDestino = slots[slot];
            }
            Tween tween = GetTree().CreateTween();
            // Offset levemente acima do campo para não clipar
            tween.TweenProperty(Seletor3D, "global_position", slotDestino.GlobalPosition + new Vector3(0, 0.05f, 0), 0.1f);
            Seletor3D.GlobalRotation = slotDestino.GlobalRotation;
            await ToSignal(tween, Tween.SignalName.Finished);
        }

        /// <summary>
        /// Método principal que a IA chama para realizar a jogada completa.
        /// </summary>
        public async Task<FusionResult> RealizarJogadaIA(AIMove cardToPlay, bool isSpell)
        {
            // 1. Navega até cada carta que será fundida
            await ExecutarMovimentoVisual(cardToPlay.IndexCard, noCampo: false);
            
            // "Clica" na carta (simula a seleção visual de fusão)
            List<CardUi> cartaUi = new List<CardUi>();

            foreach(var slotCard in cardToPlay.IndexCard)
                cartaUi.Add(MaoControlIA.GetCarta(slotCard));

            _anim._cartasSelecionadasParaFusao.AddRange(cartaUi);
            if(cartaUi.Count() == 1)
            {
                await _anim.AnimaCartaParaCentroIA(cardToPlay.IndexCard.First()); 
                await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);        
                await _anim.AnimaCartaParaMao(cardToPlay.IndexCard.First());                
            }

            await Tools.TransitionTo(CameraField, 0.5f, _transitionCam, false);
            Seletor3D.Visible = true;

            var stringIds = cardToPlay.CardToPlay.Select(i => i.Id.ToString()).ToList();
            var pChain = ProcessChain(string.Join(",", stringIds));
            if(pChain.FusaoAconteceu || !pChain.FalhaEquip)
                await _anim.AnimaFusao(pChain);
            
            // 3. Navega pelo campo
            if (!pChain.MainCard.IsSpellTrap())
            {
                CleanUpCrew();
                var slot = await ExecutarMovimentoVisual(cardToPlay.IndexCard, noCampo: true, isSpellTrap: isSpell, carta: pChain.MainCard, facedown: !pChain.FusaoAconteceu);            
                pChain.WorldPos = slot;
            }
            else
            {
                var cartaSpell = cartaUi.FirstOrDefault(x => x.carta.Id == pChain.MainCard.Id);
                if(cartaSpell != null)
                {
                    await Tools.TransitionTo(CameraHand, 0.5f, _transitionCam, false);
                    await cartaSpell.AtivaSpellAnimation(_anim.ScrenCenter());
                    await Tools.TransitionTo(CameraField, 0.5f, _transitionCam, false);
                }
            }

            // 4. Finaliza e limpa cursores
            IndicadorTriangulo.Visible = false;
            Seletor3D.Visible = false;
            _indiceVisualMao = 0; // Reset para a próxima vez
            _indiceVisualCampo = 0;

            return pChain;
        }

        public void ConfigurarSlots(
			Godot.Collections.Array<Marker3D> monstrosAliados, 
			Godot.Collections.Array<Marker3D> monstrosInimigos,
			Godot.Collections.Array<Marker3D> magiasAliados,
			Godot.Collections.Array<Marker3D> magiasInimigos)
		{
			this.SlotsCampo = monstrosAliados;
			this.SlotsCampoIni = monstrosInimigos;			
			this.SlotsCampoST = magiasAliados;
			this.SlotsCampoSTIni = magiasInimigos;						
			
		}

        public async Task Instancia3D(Marker3D slotDestino, Cards fusao, bool facedown = false)
        {
			bool IsEnemy = slotDestino.Name.ToString().Contains("Ini");
			var novaCarta3d = await Tools.InstanciaNodo(slotDestino, CameraHand: CameraHand);			
			novaCarta3d.Setup(fusao, _indiceVisualCampo, IsEnemy, facedown, slotDestino.Name);			
			await _anim.AnimaCarta3DParaCampo(novaCarta3d, IsEnemy);	
		}

        public void CleanUpCrew()
		{
			foreach (var carta in _anim._cartasSelecionadasParaFusao)
			{
				if(IsInstanceValid(carta))
					carta.QueueFree();
			}	
		}

        void AlternarDefesa()
		{
			
			var slotDestino = SlotsCampo[_indiceVisualCampo];

			if(gameLoop.MonsterHasAttacked(slotDestino.Name)) return;

			var isEnemy = slotDestino.Name.ToString().Contains("Ini");

			var pegou = Tools.PegaNodoCarta3d(slotDestino.Name);

			if (pegou == null)
				return;

			var rotacao = pegou.Rotation;

			if (pegou is Carta3d nodo){
				nodo.Defesa = !nodo.Defesa;
				gameLoop._gameState.CurrentPlayer.Field.BotaDeLadinho(nodo.markerName, nodo.Defesa);
			}

			if (!isEnemy)
			{
				if (rotacao == new Vector3(0,0,0))
					pegou.Rotation = new Vector3(0, 1.5707964f, 0);
				else
					pegou.Rotation = Vector3.Zero;
			}
			else
			{
				if (rotacao == new Vector3(0,3.14f,0))
					pegou.Rotation = new Vector3(0, -1.5707964f, 0);
				else
					pegou.Rotation = new Vector3(0,3.14f,0);
			}
		}
    }
}