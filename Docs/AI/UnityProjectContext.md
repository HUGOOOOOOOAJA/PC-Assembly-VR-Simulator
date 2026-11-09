# Contexto persistente do projeto

Atualizado em 2026-11-09. Simulador educacional de montagem de PC em VR. O usuário confirmou a correção das mãos rígidas no headset. Em 09/11 pediu ajustes da linha e do assentamento da RAM, detalhados na seção final. O último checkpoint local registrado é 99fe0f5.

## Objetivo e limites

Simulador educacional de montagem de computadores em realidade virtual para Meta Quest. O jogador manipula componentes de PC com as mãos, incluindo a memória RAM e seu encaixe na placa-mãe. O projeto ainda é um protótipo em desenvolvimento; não pressupor que todos os componentes de um PC já possuem lógica de montagem.

O usuário pretende continuar o desenvolvimento com o assistente e pede continuidade entre conversas. Este documento é a referência persistente; consultar os arquivos atuais antes de agir, pois o estado pode mudar. Não depender apenas da memória do chat.

Preferência expressa em 29/10: quando um teste precisar da participação do usuário com o VR, perguntar antes se ele pode usar o headset e explicar na própria pergunta o que deverá fazer. Aguardar a resposta antes de iniciar/reiniciar esse ensaio. Testes simulados não substituem essa confirmação física.

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

## Travas dos slots adicionadas em 22–23/10/2026

- Cada slot possui duas travas independentes, totalizando oito, no grupo `MSI_Z790/RAM_Latches`. Começam fechadas em cada Play. A pessoa faz pinça na ponta da trava e move a mão para fora do slot; a peça gira presa à dobradiça, sem ser retirada da placa.
- `Assets/Scripts/RamLatch.cs` deriva de XRBaseInteractable. Rotaciona apenas o filho Handle no eixo Y local, entre 0 e 55 graus. Ao soltar, fixa no extremo mais próximo; cancelamento de seleção fecha se ainda não atingiu a abertura. Seleção distante por ray não é aceita: NearFarInteractor precisa de alvo no caster próximo. XRDirectInteractor utiliza a detecção próxima normal do XRI. Slot ocupado impede manipular suas travas.
- `Assets/Scripts/RamSlotLatches.cs` exige duas referências válidas, ativas, abertas e já soltas. `RamSocketFilter` consulta esse componente antes de permitir hover/encaixe novo; ausência de configuração bloqueia. Uma RAM já instalada não é ejetada por essa regra. As verificações anteriores de orientação, posição e velocidade permanecem.
- O OBJ original da placa-mãe foi preservado. A cena usa um mesh derivado em `Assets/Generated/RamLatches/MSI_Z790_RamLatchSlots.asset`, com somente os ressaltos superiores das extremidades DDR5 rebaixados até Z local 0.097981. Materiais e outras partes da placa mantidos. O novo mesh das travas e seu material de polímero escuro ficam nessa mesma pasta.
- A geometria e as dobradiças são uma aproximação interativa do modelo simplificado, não uma reprodução mecânica exata da placa física. Os colliders de interação ficam na cabeça de pegada (centro Z=0.125, tamanho 0.085×0.075×0.054 em unidades locais). Colliders cobrindo toda a haste penetravam o volume da RAM mesmo abertos; os colliders finais não penetram a RAM alinhada nos testes. O ajuste visual detalhado da base/recorte ainda pode ser refinado com headset.
- `Assets/Scripts/RamAssemblyHint.cs` controla um painel TextMeshPro no mundo, orientado por beginCameraRendering para cada câmera Game/Scene View. Instrui abrir as duas travas, depois alinhar os contatos dourados; mostra também os quatro slots ocupados. Não ensina pressionar nem promete som nesta versão. O filho Instrução precisa de rotação local identidade: um giro adicional Y=180 salvo na cena espelhava o texto mesmo com o pai orientado corretamente. A rotação foi restaurada e a leitura conferida no Game View.
- Ferramentas de reconstrução mantidas em `Assets/Editor/RamLatchSceneBuilder.cs` e `RamHintSceneBuilder.cs`. O primeiro comando recusa duplicar um conjunto existente. Não o executar para substituir/recriar automaticamente uma cena já configurada.
- Pressionar com dedo/mão, fechamento automático após pressão e áudio “tec” NÃO foram implementados: ficam para a próxima etapa, conforme combinado. Por enquanto, após abrir as duas travas o encaixe usa o comportamento anterior do socket; as travas permanecem abertas.
- Validação sem headset: teste inicial mostrou os quatro sockets aceitando RAM sem abrir travas. Verificador temporário executou 269 verificações por ciclo e, após acrescentar a regressão do texto espelhado, 270 verificações em cada um de dois ciclos finais de Play, sem falhas: fechadas, só uma aberta, ambas abertas, fechamento de slot vazio, limites de ângulo, pivôs fixos, mensagens e orientação local do texto, 16 combinações RAM/slot, regressões de orientação/distância/arremesso, ausência de penetração nos colliders abertos, quatro capturas automáticas, ocupação, retirada/reinserção e soltura lenta por interactor simulado com gravidade. O verificador temporário foi removido do projeto ao concluir; não é componente de gameplay. Código do verificador e relatórios ficam na pasta work do chat de desenvolvimento.
- O ensaio inicial precisou sincronizar tanto Transform quanto Rigidbody ao teleportar a RAM original; antes disso ela permanecia na posição inicial no teste. Não foi alterado o filtro para esconder esse problema do verificador. Um SelectExit aplicado a uma RAM ainda não selecionada também foi corrigido no verificador.
- Aparência do painel e das RAMs instaladas revisada no Game View. Continua pendente validar o gesto real com cada mão, a facilidade de pegar as travas pequenas, legibilidade/conforto no headset e build Android. Avisos de ausência do subsistema de mãos/eye tracking são esperados sem headset.
- Ao iniciar esta tarefa havia alterações não salvas do usuário na luz da bancada (intensidade 10, X=1.467 e UniversalAdditionalLightData). Foram salvas e preservadas. Não houve alteração intencional do rig, da correção PinchPointFollow, da mesa, da posição da placa ou do cenário. Unity também atualiza metadados OpenXR e dependências do scene template durante importação/salvamento.

## Encaixe guiado por dois polegares (28/10/2026)

- O usuário confirmou no headset que a etapa anterior de abrir travas e posicionar RAM funcionou. A nova interação foi aprovada com desenho e plano em `Docs/superpowers/`: um polegar de cada mão nas extremidades superiores, movimento alinhado para baixo, assentamento e fechamento das duas travas; nenhuma retirada após instalar naquela sessão.
- `RamThumbTracking` lê somente ThumbTip do XR Hands em Dynamic, exige juntas atualizadas e mãos rastreadas, converte pela Camera Offset do rig e invalida dados em trackingLost, desativação, troca/parada de subsistema ou falta de atualização por 0.12 s. Não mede força real.
- `RamPressGesture` exige aproximação por cima (12 mm), contato e descida de 6 mm dos dois polegares, janela bilateral 0.25 s e pelo menos 0.06 s após contato. Dados inválidos/antigos, entrada lateral/inferior, repouso, uma mão, troca de extremidade, movimento lateral e recuo do polegar invalidam o histórico. Histerese de recuo: 1.5 mm.
- `RamPressSlot` mantém estados Empty, Positioned, Seating e Installed. Cria attach temporário só em runtime, 8 mm mundiais acima do assento original, mantendo seleção XRI para sustentar a RAM. Antes da pressão permite retirar e recolocar. Após confirmação anima o mesmo attach até o original em 0.25 s e fecha as travas por `CloseForInstallation`. Originais RAM_Attach não são deslocados em edição. Ao reativar o componente, reconcilia a seleção atual do socket, limpa cache se vazio e preserva assentamento em curso quando o mesmo módulo continua selecionado.
- O trigger original terminava praticamente rente ao assento (~32 micrômetros acima). Só em runtime ele é estendido 10 mm no eixo da inserção para alcançar o apoio; base original mantida e center/size restaurados na destruição. Não foi afrouxado RamSocketFilter: poses, velocidades, frame de soltura e atalho de retenção pelo próprio socket permanecem.
- Contato usa raio máximo configurado 18 mm, limitado em cada módulo à meia espessura real +3 mm (aproximadamente 9 mm neste modelo). Raio 18 mm sem esse limite permitiria pressionar uma RAM/slot e confirmar a vizinha, pois o passo é 13.9 mm. `RamPressCoordinator` escolhe no máximo um slot para os dois polegares, rejeita empates a menos de 1.5 mm e mãos apontando slots distintos; módulos já instalados também bloqueiam o foco que iria para um vizinho.
- `RamInstallationLock` registra filtro no módulo antes da confirmação e, ao travar, só permite seleção pelo socket proprietário. Não desabilitar o XRGrabInteractable para isso, pois perderia sua seleção. Validação usa XRInteractionManager.IsSelectPossible, que consulta filtros de ambos os lados; CanSelect apenas do interactor não consulta o filtro do módulo.
- `RamPressDemonstration` anima um exemplo independente com dois polegares estilizados transparentes, unhas, RAM e duas travas. Nenhum collider, Rigidbody ou interagível; não fornece entrada ao gesto. Ciclo 6 s, ocultando o exemplo antes de reiniciar. Painel avisa: "Pressione firme, gradual e uniformemente. Se estiver torta ou não encaixar, pare e reposicione. Não force." `RamAssemblyHint` passa a contar Installed, não apenas sockets ocupados.
- Builder `Assets/Editor/RamPressSceneBuilder.cs`, menu Configure RAM Press, configura tracker/coordenador, quatro slots/quatro bloqueios e exemplo. Recusa Play e evita duplicação; materiais em `Assets/Generated/RamPressDemo/`. Não reexecutar os builders antigos para substituir a configuração. Não alterar referências/poses dos objetos reais para enquadrar a demonstração.
- Evidência final: 24 casos de gesto/tracking, nove verificações de reconexão usando dois subsistemas XRHand reais com provider temporário e 12 verificações de demonstração/configuração passaram em Edit. Depois da revisão independente e sua correção, dois ciclos completos de Play simulado passaram, cada um com 74 verificações novas, 270 regressões e uma verificação do aviso de apoio provisório, sem FAIL/ERROR. Incluem placa rotacionada/não uniformemente escalada, retirada na primeira amostra que poderia confirmar, troca de módulo durante desativação do slot e restauração do trigger/attach. Baselines falharam antes da implementação. Foram reproduzidas e corrigidas falta de alcance do trigger, recuo do polegar, acionamento de vizinhos e cache de módulo na reativação. O verificador foi corrigido para consultar filtros dos dois lados e respeitar frame de soltura/recycle delay de 1 s, sem afrouxar regras do produto. Relatórios pós-revisão: ram-press-postreview1/2.txt e ram-press-regression-postreview1/2.txt em work do chat.
- Revisão independente concluída em contexto novo, somente leitura. O achado de cache antigo na reativação foi reproduzido (B selecionada enquanto cache ainda A), corrigido e coberto pelos dois ciclos finais; nenhum achado de código pendente. Os quatro verificadores temporários e seus .meta foram copiados para work e removidos de Assets; builder de produção e materiais permanecem. Unity recompilou após a limpeza, Console sem erros/avisos e cena salva fora de Play. HEAD segue 99fe0f5, sem novo commit/push.
- Animação e leitura conferidas no Game View. O novo painel fica 0.58 m acima e 0.38 m ao lado do primeiro slot; foi elevado para não sobrepor o texto anterior movido pelo usuário. Os 46 Transforms/RectTransforms locais anteriores mantêm posição, rotação e escala do backup. Comparados todos os 242 blocos YAML anteriores: nenhum removido; as únicas mudanças são oito inclusões de componente nos objetos RAM/slots, ligação do hint ao coordenador e duas novas raízes de cena. Isso inclui a preservação dos overrides do rig/câmera, colliders, luzes, cenário e modelos anteriores. OBJ original e PinchPointFollow sem alterações. A câmera usada para inspeção foi ajustada apenas durante Play e restaurada ao parar.
- Ainda é necessário teste real dos dois polegares no headset, sobretudo oclusão, conforto e tolerância de contato. Conexão do headset durante um ensaio com snapshots simulados não equivale a validar o gesto real. Não foi executado build Android. Colisão do corpo com mesa, mãos físicas completas, áudio e dano ficam para depois. Parar/iniciar Play reinicia a montagem, sem persistência.
- Havia ajustes locais do usuário em SampleScene/OpenXR e cena não salva ao retomar; foram salvos e copiados para `work/SampleScene.before-press.unity` e `work/OpenXRPackageSettings.before-press.asset`. Preservar também a posição modificada pelo usuário do texto Instrução. Não reverter esses arquivos ao Git para limpar metadados.

## Ajuste da pressão após teste do usuário (29/10/2026)

- O usuário relatou que pressionar no VR não funcionou e pediu uma posição um pouco mais baixa, mantendo parte dos contatos dourados exposta antes do encaixe. Também pediu nenhuma imagem/tela explicativa e removeu os textos manualmente. Essas remoções foram salvas e preservadas antes da correção.
- Diagnóstico: na RAM de ~215 mm, os pontos antigos a ±35% do comprimento ficavam ~32 mm para dentro das pontas reais, fora do raio transversal de ~9 mm. Além disso, não havia movimento até Complete; pressão parcial parecia não fazer nada.
- Apoio reduzido de 8 para 4 mm acima do assento final, curso reconhecido de 6 para 4 mm. Assento original e posição final mantidos. `RamPressGesture.Progress` expressa o menor progresso dos dois polegares. Durante pressão válida o mesmo attach desce até 90% do curso, com retorno ao apoio se perder/desalinhar rastreio; confirmação termina o assentamento e fecha/bloqueia as duas travas.
- As áreas de cada extremidade agora têm meia extensão longitudinal de comprimento×0.15 +6 mm (~38 mm), mantendo meia largura estreita de espessura/2 +3 mm (~9 mm) para não alcançar o slot vizinho. Drift transversal permitido até 80% dessa meia largura; deslocamento longitudinal após contato limitado a 12 mm. Aproximação por cima, duas mãos distintas, pose/velocidade, arbitração de vizinhança e propriedade exclusiva continuam exigidos.
- Zonas são calculadas na pose neutra do apoio, usando as dimensões mundiais do collider e compensando a pose atual do attach. A própria descida não desloca o objetivo do gesto. O XRI instalado consulta o attach do socket durante o processamento e movimenta a RAM selecionada; nenhuma colisão física geral de mãos/corpo foi adicionada.
- Raízes remanescentes de orientação/demonstração desativadas; assets preservados. Removidas as chamadas do builder que recriavam visuais. Não executar builders antigos para restaurar telas que o usuário removeu.
- Verificação atual: 26 casos de gesto/configuração, 261 regressões físicas/filtros/travas e 45 verificações de movimento passaram, sem FAIL/ERROR. Nove testes antigos de texto foram retirados do verificador porque os textos foram removidos pelo usuário. O teste novo reproduziu antes a ausência de movimento nas pontas reais. Revisão independente sem achados funcionais pendentes. Todos os 262 blocos anteriores da cena preservados, só parâmetros dos quatro slots e flags de ativação das duas raízes visuais mudaram; rig, colliders anteriores, luzes e poses intocados.
- Usuário autorizou teste real em 29/10 após pergunta com instruções; Play limpo iniciado para conferir descida e travas. Resultado físico ainda não confirmado. Relatórios/backups/verificadores em work/ram-press-2026-10-29 do chat. Limpeza dos verificadores temporários aguarda fim do ensaio para não interromper o VR.

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

## Continuidade — faixa de pressão, recorte e mãos físicas (29/10/2026)

- Evidência real anterior: o usuário confirmou **"Desceu e travou"** para a primeira RAM. A captura original está preservada em `work/ram-press-2026-10-29/live-pressure-first-success.csv` do chat. Dynamic e BeforeRender estavam atualizando; não mudar a fase do rastreador por suposição.
- Após o usuário pedir pressão em qualquer parte da RAM, `RamPressGesture` ganhou modo de superfície completa, usado nos quatro slots: dois polegares distintos, separados longitudinalmente por pelo menos 15 mm, em qualquer parte da borda superior. `RamPressCoordinator` arbitra pela linha central limitada da superfície, mantendo proteção de vizinhos/empates. Aproximação por cima e curso bilateral de 4 mm continuam obrigatórios; uma mão, entrada inferior, dados inválidos e movimento parado continuam recusados. Após aproximação válida há tolerância de penetração de até 24 mm para não perder o gesto em amostras rápidas. Esta faixa completa ainda aguarda confirmação no headset.
- O recorte dourado estava deslocado ~9.251 mm da chave pintada. Medição do mesh importado encontrou centro local X=+0.017158987; referência da chave na placa X~0.7704. Somente os quatro RAM_Attach receberam rotação local Z=180° e X=-0.016674101; eixo de inserção e altura final foram preservados. Erro geométrico calculado zero nos quatro slots, com incerteza da textura ~0.25 mm. Aparência final ainda precisa de confirmação no VR.
- Pedido de baixar mais a RAM permanece pendente, para depois do ensaio solicitado pelo usuário. Apoio e curso atuais continuam 4 mm; não interpretar a correção de X/rotação como mudança de altura.

### Mãos físicas — implementação inicial

- O usuário escolheu física completa, aprovou o desenho e exigiu preservar a linha. A implementação mantém esqueleto rastreado, XRHandSubsystem, Aim Pose, PinchPointFollow, casters, pinça e RamThumbTracking como fontes originais. Não escrever poses físicas nesses caminhos.
- `PhysicalHandTracking` copia poses completas em Dynamic, converte pelo pai do root do skeleton driver, exige rastreio recente (0.12 s), invalida ausência de juntas/parada/perda de tracking e reconecta ao subsistema substituto. BeforeRender não avança os alvos físicos.
- `PhysicalHandRig` cria uma palma e 15 segmentos dinâmicos, cápsulas/caixa aproximadas em metros, sem gravidade. Juntas têm translação travada e rotação livre; forças e torques limitados acompanham os alvos. Usa solver 12/4, ContinuousSpeculative e exclusão interna da mesma mão. Não há resistência física real na mão do usuário.
- Corpos e colliders são criados em runtime, em raiz separada de escala identidade na mesma cena. Tracking inválido suspende colisões/forças. Distância de mais de 30 cm até o alvo suspende a recuperação ativa. Retorno só habilita dinâmica após verificar pose livre de sólidos: ComputePenetration exige shapes habilitados, então eles são habilitados apenas durante a consulta com todos os corpos ainda cinemáticos e desabilitados novamente se houver sobreposição.
- `PhysicalHandVisual` copia somente Transforms e SkinnedMeshRenderer, remapeia ossos e aplica poses físicas em ordem pai-filho. O renderer original usa forceRenderingOff temporário, restaurado na desativação; rastreadores e referências de affordance originais permanecem. MaterialPropertyBlocks são copiados. A cópia não contém drivers XR/PokeDisplacer concorrentes. Visual físico é aplicado também em BeforeRender; não tentar sobrescrever o esqueleto original em LateUpdate, pois XRHandSkeletonDriver o atualiza novamente em BeforeRender.
- `PhysicalHandGrabCollision` preserva IgnoreCollision anteriores e ignora os colliders das peças selecionadas por interatores da própria mão. Uma seleção restante mantém a exclusão. Na soltura aguarda pelo menos 0.12 s e separação geométrica antes de restaurar os pares; ao encerrar restaura somente o estado anterior.
- `PhysicalHandController` está adicionado aos dois QuestVisuals da cena e liga rastreio/física/visual/seletores da respectiva mão. HandVisualizer continua controlando ativação de mãos/controles; a desativação da fonte elimina os proxies e devolve o renderer original.
- Camada Unity **Physical Hands**, índice 8. As máscaras de raycast, sphere/poke e bloqueio da cena excluem somente esse bit, preservando os outros. Proxies não são interactables. Builder permanente `Assets/Editor/PhysicalHandsSceneBuilder.cs`, menu Configure Physical Hands; não altera prefabs de samples nem posições do rig.
- Evidência automática final: 17 ensaios físicos em PhysicsScene de prévia isolada, 30 de visual/pegada/configuração e 14 de rastreio/ciclo do controlador passaram; incluem palma/dedo contra sólido, empurrar corpo solto, perda/retorno, retorno dentro de sólido, pose inválida, pele independente, release sobreposto e reconexão. As regressões de RAM passaram: 31 em Edit, 261 de filtros/travas e 45 de movimento/assentamento dos quatro módulos em Play simulado (15:26). Esses resultados não validam conforto ou estabilidade com mãos reais.
- Comparação incremental da cena com o backup desta etapa: todos os 262 blocos anteriores preservados; somente a instância do rig recebeu overrides, com dez blocos adicionados para dois componentes e referências stripped. Cenário, mesa, placa, luzes e RAMs não foram reposicionados. PinchPointFollow permanece sem diff, SHA256 `3AC2AEA247F1F7201D1BB8E5B3BDF507B9405E2285CC6C522B353330FD4181B5`.
- Revisão independente encontrou um problema de ciclo: HandVisualizer pode desabilitar XRHandMeshController/renderer sem desabilitar driver, deixando colliders invisíveis. Reproduzido antes de corrigir, depois 14/14 verificações passaram. Update, FixedUpdate e Render agora usam a mesma condição CanSimulate (rastreio e fonte/mesh-controller/renderer ativos e mão rastreada); forceRenderingOff não é parte da condição, pois o proxy o usa para ocultar só a pele original. O ensaio aciona os métodos reais do controlador num fixture de Edit; não representa uma troca real de modalidade no headset.
- Desenho, plano, backups, ledger e relatórios estão em `work/2026-10-29-physical-hands-*.md` e `work/physical-hands-2026-10-29/` do chat. Verificadores temporários de geometria/regressão e os novos ensaios foram arquivados em `validators/` e retirados de Assets/Editor. Builders permanentes preservados. PhysicalHandRuntimeDiagnostics e RamLivePressureDiagnostics permanecem temporariamente para registrar o próximo Play em CSV local, sem UI; retirar depois do ensaio, fora de Play.
- Alterações sem commit/push. Revisão de código final concluída e achado importante coberto por RED→GREEN. Falta ensaio real de linha/pinça/contato/RAM e medição de custo no headset; build Android não executado. Foi enviada pergunta ao usuário com os passos do ensaio; não iniciar Play que dependa dele antes de resposta.

## Correção após reprovação da mão deformável — 04/11/2026

- O primeiro ensaio real das mãos físicas foi feito e o usuário reprovou dedos que deformavam e escorregavam na mesa/RAM. Os resultados automáticos da seção anterior são históricos e não validaram essa sensação. CSVs desse ensaio estão arquivados em work/physical-hands-2026-10-29/live-*-first-*.csv do chat.
- Política confirmada: manter dois polegares, um de cada mão. O contato físico contínuo em pontos separados do topo da mesma RAM apoiada deve iniciar o encaixe. Nenhuma imagem/painel explicativo novo. Sem commit/push.
- PhysicalHandRig agora usa UM Rigidbody dinâmico por mão com 16 colliders compostos (palma + segmentos dos dedos), sem joints dinâmicos por dedo. Pose dos dedos relativa ao punho vem do rastreio real; a colisão move a mão inteira e não estica ossos. Atrito alto e centro de massa/inércia fixos. PhysicalHandVisual sobrescreve só o punho e preserva a articulação rastreada. O teste reproduziu antes deformação de 58.9 mm/134.85 graus; após correção mediu cerca de 0.000000094 m/0 graus.
- PhysicalHandCollisionSource recebe OnCollisionEnter/Stay no corpo composto e preserva collider/junta/ponto/normal/tempo. PhysicalHandController retransmite contatos apenas com rastreio e fonte física ativos; perda/ocultação suspende os colliders e o visual. Fontes de linha/pinça e PinchPointFollow permanecem intactas.
- RamPressCoordinator está configurado na cena com m_UsePhysicalContacts=true e os dois PhysicalHandControllers. Movimento cru de polegares não instala nesse modo. Aceita polegares de mãos distintas, topo do mesmo collider de RAM, separação mínima 15 mm, duração 0.12 s, amostras sem intervalo maior que 0.05 s. Expira contatos antigos/inativos e rejeita ambiguidade com módulos vizinhos, inclusive já instalados.
- RamPressSlot.ClassifyPhysicalContact verifica o topo real do BoxCollider. TryBeginPhysicalSeating compartilha a transação existente de assentamento: seleção exclusiva pelo socket, travas abertas, attach estável, lock de instalação no dono correto. Mantém animação de 0.25 s, fechamento de ambas travas e impedimento de remoção. Contato inicia a animação; não estima força aplicada pelo usuário.
- Revisão independente encontrou 1 Important: gesto concluído era consumido mesmo se o attach ainda não estivesse estável. Reproduzido em retry-red.txt (4 PASS/2 FAIL). Coordenador agora reinicia o gesto ao rejeitar a transação; contato contínuo tenta de novo sem levantar os polegares. retry.txt passou 6/6, com módulo real selecionado, inicialmente 8 mm fora do apoio, depois alinhado, instalado e bloqueado no socket.
- Evidência atual automática: rigidez 9/9; contato/topo/tempo/partes inválidas 14/14; rastreio/visibilidade/reconexão 14/14; retry 6/6; regressões de encaixe/travas 261/261; Play com dois corpos de prova dinâmicos e callbacks reais de colisão nos quatro módulos 57/57. Sem FAIL/ERROR nesses relatórios. Este último ensaio cobre receptor, geometria, transação, descida e latches; não reproduz a agregação do coordenador a partir de mãos humanas. Ainda falta teste real atualizado de firmeza, deslizamento, contato bilateral, pegada e linha. Build Android/desempenho no dispositivo não verificados.
- Cena comparada com SampleScene.before.unity desta correção: 272 blocos antes/depois, nenhum removido/adicionado e nenhum fixture salvo. Só mudaram o coordenador (modo físico/referências) e a instância do rig (14 valores de SizeDelta de UI recalculados pelo editor para zero). Poses/colliders/cenário/mesa/placa/luzes/RAM_Attach preservados. PinchPointFollow SHA256 continua 3AC2AEA247F1F7201D1BB8E5B3BDF507B9405E2285CC6C522B353330FD4181B5.
- Artefatos e verificadores temporários arquivados em work/rigid-contact-2026-10-29/ do chat, incluindo verification-summary.json e scene-comparison.json. Os cinco validadores foram retirados de Assets/Editor fora de Play. Builders permanentes preservados. Dois loggers temporários permanecem para o próximo teste e gravam live-hands.csv/live-pressure.csv nessa nova pasta; o CSV de pressão inclui idade e classificação do contato de cada polegar. Retirar loggers somente depois de parar o teste.
- Altura final extra da RAM continua pendente da avaliação do usuário. Apoio/curso atuais de 4 mm e alinhamento do recorte dourado foram preservados. Pedir e aguardar nova autorização de disponibilidade antes de Play que dependa do usuário no headset.

## Mira e profundidade da RAM em 09/11/2026

O usuário relatou de novo a falha da linha e a faixa dourada acima do slot depois de pressionar. O CSV do ensaio anterior registrou RAM_3 instalada, com erro zero em relação ao assento, o que isolou o problema de profundidade na pose final. O assento anterior coincidia com o topo medido da guia do slot. No mesh e na textura da RAM, o percentil 98 das amostras douradas nessa região ficou a 4,81 mm da base.

- Nos quatro RAM_Attach, o assento desceu 5,5 mm ao longo do eixo de inserção. O recorte manteve o alinhamento lateral. m_PreviewHeight passou de 4 para 9,5 mm em relação ao novo assento, preservando os 4 mm de apoio acima da referência antiga. A verificação geométrica estima a faixa principal a 0,69 mm abaixo da boca do slot depois do encaixe. A cena salva inclui a disposição escolhida pelo usuário, com as outras RAMs embaixo da mesa e as travas visíveis conforme ele simplificou a apresentação.
- PinchPointFollow usa a mira nativa válida quando disponível. As juntas do punho e da base do indicador mantêm a direção quando a pose de mira está indisponível ou é incompatível com a mão. A atualização de direção continua quando falta uma ponta do dedo. O caster e o início visual partem da mesma pose estabilizada, e a origem visual preserva seu deslocamento de 15 mm. A inscrição de eventos das duas mãos continua nos vínculos persistentes do rig.
- Ensaio no editor antes da correção: oito falhas reproduzidas, quatro da profundidade e quatro de mira. Depois: 21 PASS, zero FAIL/ERROR, com poses simuladas das duas mãos e medição dos quatro slots. Relatórios e backups em work/ray-seat-2026-11-09 do chat. A confirmação da aparência e da linha com mãos reais é registrada separadamente.
Em Play iniciado pelo usuário, a RAM_3 passou por Positioned, Seating e Installed no RAM_SLOT_1. O CSV arquivado em after-live-pressure.csv confirmou apoio de 9,5 mm, chegada ao novo assento com erro de 0,0 mm e retenção pelo socket. Verificação automatizada da mira permaneceu no editor. Os dois verificadores temporários foram arquivados fora de Assets.

### Retorno do teste no headset e altura de apoio

O usuário confirmou que a RAM fica encaixada corretamente depois da pressão, mas ainda fica alta antes dela. A altura de apoio em SampleScene foi reduzida de 9,5 para 5,5 mm acima do assento final. Isso coloca a base da faixa dourada na boca do slot, com os contatos expostos antes da pressão. A posição final dos quatro assentos e a disposição de apresentação foram preservadas. A comparação com SampleScene.before-preview.unity confirmou que apenas os quatro m_PreviewHeight mudaram.

O usuário relatou que a linha ainda falha com mãos reais. HandRayLiveDiagnostics.cs é temporário e registra, por mão, a pose rastreada, a origem usada pelo caster, o estado da curva e a direção renderizada em work/ray-seat-2026-11-09/ray-live-*.csv. O script foi importado e a cena recarregada. Aguardar confirmação de disponibilidade antes de iniciar Play para o ensaio no headset. As poses simuladas do teste anterior não explicam a falha observada no dispositivo.

### Captura da linha com as mãos reais

Em ray-live-151556.csv, a direção visível seguiu a direção do caster (diferença média de 0,27° e 0,36° com a linha ativa). A mira escolhida, contudo, desviava do eixo das juntas em até 98,61°. Houve 331 amostras acima de 45° na esquerda e 268 na direita, de 515 e 512 amostras rastreadas com a linha visível. A origem Meta era aceita antes da direção das juntas, o que provocava desvios e mudanças bruscas ao passar pelo limite angular.

PinchPointFollow passou a priorizar punho e base do indicador para a direção em cada mão. A mira nativa validada permanece como alternativa quando essas juntas estão indisponíveis. A orientação foi verificada repetindo no editor uma pose semelhante à capturada, com falha nas duas mãos antes e 23 PASS sem falhas depois. Os quatro apoios da RAM também foram medidos na boca do slot nesse ensaio. Diagnóstico temporário permanece até a nova verificação no headset. Ao sair do Play anterior apareceu MissingReferenceException em PhysicalHandRig.Suspend ao desabilitar um collider já destruído. O erro ocorre na saída do ensaio e é separado da causa de mira confirmada.
