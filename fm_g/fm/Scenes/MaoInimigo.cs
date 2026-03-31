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
        public async Task<string> ExecutarMovimentoVisual(int indiceAlvo, bool noCampo = false, bool isSpellTrap = false, Cards carta = null)
        {
            if (!noCampo)
            {
                IndicadorTriangulo.Visible = true;
                while (_indiceVisualMao != indiceAlvo)
                {
                    _indiceVisualMao += (_indiceVisualMao < indiceAlvo) ? 1 : -1;
                    await AtualizarPosicaoIndicadorInimigo();
                    await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);
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
                    AtualizarPosicaoSeletor3DInimigo(slotsAlvo);
                    if(slotDestino == -1)
                    {
                        slotvazio = true;
                        await Instancia3D(slotsAlvo[_indiceVisualCampo], carta);
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

        private void AtualizarPosicaoSeletor3DInimigo(Godot.Collections.Array<Marker3D> slots)
        {
            if (Seletor3D == null || slots.Count == 0) return;

            var slotDestino = slots[_indiceVisualCampo];
            Tween tween = GetTree().CreateTween();
            // Offset levemente acima do campo para não clipar
            tween.TweenProperty(Seletor3D, "global_position", slotDestino.GlobalPosition + new Vector3(0, 0.05f, 0), 0.1f);
            Seletor3D.GlobalRotation = slotDestino.GlobalRotation;
        }

        /// <summary>
        /// Método principal que a IA chama para realizar a jogada completa.
        /// </summary>
        public async Task<FusionResult> RealizarJogadaIA(List<Cards> indicesMao, Cards carta, int slotIndex, bool isSpell, bool faceDown)
        {
            // 1. Navega até cada carta que será fundida
            await ExecutarMovimentoVisual(slotIndex, noCampo: false);
            
            // "Clica" na carta (simula a seleção visual de fusão)
            var cartaUi = MaoControlIA.GetCarta(slotIndex);
            await _anim.AnimaCartaParaCentroIA(slotIndex); 
            await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);        
            await _anim.AnimaCartaParaMao(slotIndex);
            await Tools.TransitionTo(CameraField, 0.5f, _transitionCam, false);

            // 3. Navega pelo campo
            var slot = await ExecutarMovimentoVisual(slotIndex, noCampo: true, isSpellTrap: isSpell, carta: cartaUi.carta);
            
            // 2. IA "aperta" Accept - Anima para o centro
            // (Aqui você usaria sua lógica de fusão existente no AnimationP)v
            var pChain = ProcessChain(cartaUi.carta.Id.ToString());
            pChain.WorldPos = slot;
            await _anim.AnimaFusao(pChain);


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
			
			GD.Print("MaoJogador: Slots redefinidos com sucesso via GameLoop.");
		}

        public async Task Instancia3D(Marker3D slotDestino, Cards fusao){
			bool IsEnemy = slotDestino.Name.ToString().Contains("Ini");
			var novaCarta3d = Tools.InstanciaNodo(slotDestino);			
			if(IsEnemy){
				GD.Print(slotDestino.GlobalRotation.ToString());
				Vector3 rota = new Vector3(-0, 1.5707964f, 0);
				novaCarta3d.GlobalRotation += slotDestino.GlobalRotation + rota;
			}			
			novaCarta3d.Setup(fusao, (int)_indiceVisualCampo, IsEnemy, true, slotDestino.Name);			
		}
    }
}