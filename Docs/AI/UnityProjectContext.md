# Contexto persistente do projeto

Atualizado em 2026-10-22. Snapshot do jogo: commit `43184c4b660d161638f9b4a100a5f8a2ffdc1ec8`.

## Objetivo e limites

Simulador educacional de montagem de computadores em realidade virtual para Meta Quest. O jogador manipula componentes de PC com as mãos, incluindo a memória RAM e seu encaixe na placa-mãe. O projeto ainda é um protótipo em desenvolvimento; não pressupor que todos os componentes de um PC já possuem lógica de montagem.

O usuário pretende continuar o desenvolvimento com o assistente e pede continuidade entre conversas. Este documento é a referência persistente; consultar os arquivos atuais antes de agir, pois o estado pode mudar. Não depender apenas da memória do chat.

Preservar o cenário, a mesa, a placa-mãe, os objetos importantes e a conexão da RAM durante mudanças em XR. Não substituir a cena inteira por uma cena de demonstração. Novas mudanças de gameplay aguardam a próxima tarefa do usuário.

## Repositório e checkpoint

- Pasta local na data da análise: `C:/Users/hcade/Desktop/PC-Assembly-VR-Simulator`.
- GitHub: https://github.com/HUGOOOOOOOAJA/PC-Assembly-VR-Simulator
- Branch utilizada: `main`.
- Commit de segurança: `43184c4` — Salvar progresso: placa-mae, encaixe da RAM e correcao do raio das maos.
- O snapshot inclui os arquivos salvos em disco, os novos modelos e o pacote de objetos destrutíveis. Inclui também as remoções de modelos antigos e lightmaps que já estavam no diretório de trabalho; essas remoções não foram feitas durante a análise de 22/10.
- `Library`, `Temp`, `Logs`, `UserSettings` e builds são ignorados pelo Git e serão regenerados quando necessário.

## Ambiente técnico confirmado em arquivos

| Item | Versão/configuração |
| --- | --- |
| Unity | 6000.5.1f1, revisão 0d9463e84828 |
| Render pipeline | URP 17.5.0 |
| Input System | 1.19.0; activeInputHandler = 1 |
| XR Interaction Toolkit | 3.5.1 |
| XR Hands | 1.8.1 |
| OpenXR | 1.17.1 |
| XR Management | 4.6.0 |
| XR Core Utilities | 2.6.0 |
| Meta OpenXR | 2.5.1 |
| Android XR OpenXR | 1.3.1 |
| AR Foundation | 6.5.0 |

`Packages/manifest.json` e `Packages/packages-lock.json` são a fonte para dependências. A cena habilitada no build é `Assets/Scenes/SampleScene.unity`. Existe também `Assets/Scenes/BasicScene.unity`, não listada como habilitada no build analisado.

## Cena principal e modelos

Objetos identificados na serialização de `SampleScene`:

- `XR Origin Hands (XR Rig)` e `Hands Permissions Manager`.
- `XR Interaction Manager` (componente fileID 1743804406).
- `Interior Scene`, modelo `Assets/interior-scene/source/Interior Scene.fbx`.
- `Desk_fbx`, modelo `Assets/source/Desk_fbx.fbx`.
- `MSI_Z790`, modelo `Assets/motherboard-msi-z790-low-poly/source/MSI_Z790/MSI_Z790.obj`.
- Quatro memórias `RAM_1` a `RAM_4`, modelo original `tripo_convert_9a531e71-e6a4-428c-9d31-32806128088d` em `Assets/ddr5+memory+module+3d+model/`.
- `RAM_SLOT_1` a `RAM_SLOT_4`, cada um com seu filho `RAM_Attach`.
- `Cup`, `Plate`, `Bottle`, `WineGlass`, `PorcelainVase`, `Cube`, `hollow Knight` e `Hornet`.

Existem outras pastas importadas, incluindo `ddr5-ram-teamgroup-t-force-black`, `Motherboad+B450M+Steel+Legend+FBX+Low+Poly-+@ggenzet`, `hollow-knight-model` e `hornet`. A presença de uma pasta não prova que o modelo correspondente esteja sendo usado na cena.

## Conexão RAM / placa-mãe

Implementada por componentes e referências serializadas do XRI, com validação customizada em `Assets/Scripts/RamSocketFilter.cs` desde 22/10/2026.

| Elemento | Configuração salva |
| --- | --- |
| RAM | XRGrabInteractable, Rigidbody e BoxCollider |
| Camadas XRI da RAM | bits 3 = Default + RAM |
| Rigidbody da RAM | massa 1; gravidade ativa; não cinemático no estado inicial |
| Grab da RAM | rastreia posição e rotação; dynamic attach ativo; seleção única |
| RAM_SLOT_1 a RAM_SLOT_4 | filhos do transform da instância MSI_Z790; XRSocketInteractor, BoxCollider trigger e RamSocketFilter |
| Camada XRI do socket | bits 2 = RAM |
| Socket ativo | sim; recycle delay 1; sem starting selected interactable |
| Attach do socket | RAM_Attach, transform fileID 1043108920 |
| Posições locais dos slots | x 0.82, z 0.098; y 1.115080, 0.991155, 0.866493, 0.742568 respectivamente |
| RAM_Attach | posição local zero, rotação identidade, escala 1 |

`Assets/XRI/Settings/Resources/InteractionLayerSettings.asset`: Default no índice 0, RAM no índice 1, Teleport no índice 31. São camadas de interação XRI; não confundir com layers da física do Unity.

O socket usa o XR Interaction Manager explícito da cena. Os UnityEvents de seleção do socket estavam vazios: não inferir pontuação, checklist de montagem ou validação educacional adicional. Outros objetos interagíveis usam predominantemente Default (bits 1).

### Quatro memórias e validação de inserção (22/10/2026)

- Adicionadas três cópias interagíveis da RAM original sobre a mesa. Qualquer uma das quatro pode ocupar qualquer slot livre; cada socket referencia as mesmas quatro memórias no filtro.
- Os centros dos quatro slots foram medidos na geometria DDR5 do modelo da placa-mãe, não espaçados por estimativa. No modelo da RAM, os contatos dourados ficam em Z local próximo de zero e o corpo cresce em +Z. A rotação de `RAM_Attach` mantém esses contatos para baixo.
- Espessura das quatro RAMs reduzida apenas no eixo Y local: escala de 0.21511 para 0.1613325. Comprimento e altura preservados. Isso evita sobreposição dos módulos, pois o passo entre slots é aproximadamente 13.9 mm e a espessura anterior era 16.1 mm; agora é 12.1 mm.
- `RamSocketFilter` registra filtros de hover e seleção em OnEnable e remove em OnDisable. Confere os attaches do par RAM/socket, não o dynamic attach da mão. Exige rotação completa a até 12 graus e deslocamento em metros nos eixos do attach de até X=20 mm, Y=4.5 mm, Z=18 mm. A tolerância lateral menor que meio passo impede captura pelo slot vizinho. Rotações invertidas ou de lado não passam.
- Para iniciar uma seleção, exige velocidades linear até 0.4 m/s e angular até 2 rad/s. Bloqueia também o mesmo frame do evento de soltura: o XRI só aplica a velocidade de lançamento em Late, e uma verificação imediata de Rigidbody sozinha permitiria capturar um arremesso antes disso.
- RAM já selecionada pelo próprio socket continua válida durante o movimento de ajuste. A regra não bloqueia agarrar ou retirar a RAM com as mãos. `Hover Socket Snapping` continua desligado para não corrigir artificialmente uma orientação inválida antes da validação.
- Não foram alterados rig, PinchPointFollow, iluminação, posição da placa-mãe, mesa ou cenário nesta tarefa.
- Validação: reprodução inicial mostrou o socket aceitando poses erradas. Após a correção, um verificador temporário no Editor executou 157 verificações em cada um de dois ciclos consecutivos de Play, ambas com zero falhas: nove poses/velocidades nas 16 combinações RAM/slot, bloqueio no frame da soltura, velocidade de lançamento no frame seguinte, captura automática sequencial dos quatro módulos, ocupação, retenção, retirada e recolocação. Os testes atravessam a interface `IXRSelectInteractor.CanSelect`, que processa os filtros; chamar apenas o método público `XRSocketInteractor.CanSelect` não os processa.
- No primeiro diagnóstico, teleportar simultaneamente quatro corpos por Transform causou resultado físico instável no slot 3. O verificador foi corrigido para restaurar a pose do Rigidbody e montar uma memória de cada vez, como no uso esperado. Não foi afrouxado o filtro nem alterado contactOffset para esconder esse resultado.
- Não houve teste com mãos/headset ou build Android nesta tarefa. O Console em Play mostrou avisos de ausência de eye tracking e de subsistema de mãos, compatíveis com não haver headset conectado; não houve erro de compilação. O verificador temporário foi removido depois dos testes. A validação física com headset continua pendente.

## Rig XR e raio de seleção

A cena instancia `Assets/VRTemplateAssets/Prefabs/Setup/Complete XR Origin Set Up Hands Variant.prefab` (GUID 77e7c27b2c5525e4aa8cc9f99d654486). Esse variant utiliza o rig de `Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab`.

Estrutura relevante: `XR Origin Hands (XR Rig)/Camera Offset/Left Hand` e `Right Hand`, cada qual com `Near-Far Interactor`, `Aim Pose`, `Pinch Point Stabilized` e `Pinch Grab Pose`. Há também controladores, locomoção, visualizador de mãos e pós-processamento de suavização.

- Curve Interaction Caster: origem de interação em `Aim Pose` da própria mão.
- Sphere Interaction Caster: origem próxima em `Pinch Grab Pose`.
- Curve Visual Controller: origem visual em `Pinch Point Stabilized` da própria mão.
- PinchPointFollow: Target Rotation aponta para o Aim Pose correspondente.
- As ações de entrada vêm de `Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/XRI Default Input Actions.inputactions`.
- HandTracking e MetaHandTrackingAim estão habilitados em Standalone e Android no arquivo OpenXR analisado.

### Histórico da falha e correção importante

Sintoma confirmado: mãos visuais e posição da mira se moviam, mas a rotação de Aim Pose permanecia em 0,0,0, deixando a linha apontando numa direção fixa. A falha voltava em novos inícios de Play. O usuário confirmou em 22/10 que a última correção da linha funcionou.

Comparou-se com um projeto local de referência chamado `teste`, derivado do template VR. Igualar apenas os prefabs, overrides e configuração OpenXR não resolveu a recorrência. Não afirmar que Ctrl+S era a causa.

Arquivo customizado que precisa ser preservado:

`Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/Scripts/PinchPointFollow.cs`

Comportamento atual em OnJointsUpdated, com XR Hands >= 1.5:

1. Atualiza o ponto de pinça com filtragem One Euro.
2. Tenta obter a pose de mira em common gestures do XR Hands.
3. Se indisponível, lê a posição e rotação de MetaAimHand da mão correspondente quando rastreada.
4. Se ainda indisponível, calcula a direção entre Wrist e IndexProximal; usa a posição de pinça como origem.
5. Quando há uma pose válida, desativa em runtime o TrackedPoseDriver do Aim Pose para evitar que sobrescreva a orientação. Converte a pose pelo transform pai do ponto de pinça e atualiza Aim Pose e a rotação visual.
6. Mantém o bloco anterior de rotação pelo endpoint como alternativa quando a nova pose não pode ser obtida.

Esta é uma modificação de um sample importado: reimportar/substituir o sample pode apagá-la. Não mudar apenas a origem visual para Aim Pose nem trocar Cast Origin para Pinch Point Stabilized sem rever as dependências: o comportamento anterior pelo endpoint pode criar realimentação entre caster e orientação.

A direção geométrica é uma alternativa quando a pose nativa falta e pode diferir da mira nativa. O estado de habilitação do TrackedPoseDriver é alterado em runtime; futuras mudanças no ciclo de ativação das mãos devem considerar isso.

## Objetos destrutíveis e conteúdo adicional

`Assets/Yuetility-Studios/YueDestructible` contém scripts, prefabs, áudio, documentação e cenas de demonstração. `YueDestructible` calcula destruição por impulso de colisão e opcionalmente dano acumulado; ativa debris e efeitos sonoros. `YueDestructiblesRoot` controla o desaparecimento dos fragmentos. Não alterar esse pacote como efeito colateral de correções das mãos ou da RAM.

Há scripts do template e dos samples para locomoção, gestos, UI e interações. Não confundir código de demonstração importado com um sistema de objetivos já implementado no jogo.

## Validação e limites da análise

- Em sessões anteriores houve teste no Editor com headset conectado, inclusive parar, salvar e reiniciar Play após a última correção. O usuário confirma que a linha passou a funcionar.
- Em 22/10/2026 a análise foi estática: arquivos, cena, prefabs, dependências e Git. Não foi executado novo Play, build Android ou teste físico de encaixe nesta etapa de documentação.
- Não foram encontrados testes customizados de gameplay em Assets nesta inspeção.
- A montagem da RAM está configurada e foi relatada pelo usuário; o comportamento físico completo deve ser revalidado ao alterar modelos, escalas, colliders, socket ou layers.
- Não prometer que todos os objetos podem ser agarrados à distância só pela presença de XRGrabInteractable.

## Como continuar o trabalho

### Iluminação adicionada em 22/10/2026

Foi adicionado à SampleScene o grupo `Workshop Lighting`, sem mover os modelos ou alterar os componentes XR/RAM:

- Lustre Central - Quente: Point no centro do mesh `Interior_scene_complete:bulb:polySurface80`, intensidade 3.5, alcance 6.5.
- Lustre Esquerdo - Quente: Point na luminária `Interior_scene_complete:pCylinder298`, intensidade 0.85, alcance 3.2.
- Lustre Direito - Quente: Point na luminária `Interior_scene_complete:pCylinder45`, intensidade 0.85, alcance 3.2.
- Bancada - Foco Superior: Spot acima do centro da mesa, altura mundial 2.65, rotação X=90 (para baixo), intensidade 2, alcance 3.6, cone externo 78 graus e interno 48 graus.

As quatro luzes usam cores quentes, Realtime e nenhuma sombra em tempo real. O perfil `Performance URP Config.asset` foi ajustado para luzes adicionais por pixel e limite de quatro por objeto; antes elas estavam desabilitadas. O custo no headset ainda precisa ser medido. Aparência revisada no Scene View com iluminação da cena ativada, sem headset. O script temporário usado para posicionar as luzes por bounds dos modelos foi removido; as luzes estão salvas como objetos normais da cena.

Antes de cada nova tarefa: ler este arquivo, conferir git status e as referências atuais da cena. Identificar o objeto/componente da mudança, preservar o cenário e a montagem existentes, manter arquivos .meta e GUIDs. Fazer mudanças focadas e atualizar este documento quando houver alteração relevante.

Para validar alterações em XR: confirmar Editor e projeto corretos; testar cada mão separadamente, origem e direção da linha, virada do corpo, perda/retorno de rastreio, interação próxima/distante e pelo menos dois ciclos de parar/iniciar Play. Conferir Console. Validar a RAM e seu socket quando o sistema de interação for afetado.

Após mudanças de gameplay, registrar aqui o que foi implementado e o que foi realmente testado. Não executar a próxima funcionalidade até o usuário passar a tarefa.
