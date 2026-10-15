# Duall Pluvial e Reuso — GstarCAD 2025

Plugin .NET com janela própria para selecionar áreas de contribuição, registrar pavimentos tipo e acumular vazões em calhas, prumadas e coletores. Os registros acompanham os blocos ao copiá-los para outro DWG.

## Instalação

1. Baixe e extraia **Duall-Pluvial-Reuso-1.1.zip** em uma pasta permanente.
2. No GstarCAD 2025 Windows, execute `NETLOAD` e escolha **Duall.Pluvial.1.1.dll**.
3. Execute `PLUVIAL`. Os próximos passos são feitos pelos botões da janela.
4. Mantenha **D-Prumada-Plu.dwg** na mesma pasta da DLL.

Use uma sessão nova ao atualizar o plugin: DLLs já carregadas normalmente permanecem na memória até fechar o GstarCAD.

## Uso

- Informe obra, município, intensidade da chuva, TR, duração e fonte. Confirme que o desenho está em metros.
- Escolha **Pluvial** ou **Reuso**. O cálculo é igual; a prumada usa respectivamente **D-HID-PLU-TET** ou **D-HID-APV-TET**.
- Insira a prumada pelo painel. A definição **D-Prumada-Plu** é exclusiva do plugin e usa a propriedade dinâmica **PRUMADA**.
- Em **Contribuições**, selecione contornos fechados, informe acréscimos e os pavimentos efetivamente atendidos. A lista de pavimentos tipo pode ser gerada pelo painel.
- **Unir tubos no destino** acumula contribuições distintas, ignora repetições idênticas e rejeita revisões conflitantes da mesma origem/pavimento.
- Copie o bloco entre DWGs e selecione-o no painel para continuar. Os registros são cópias, sem sincronização automática com arquivos anteriores.
- Use **+ Calha** para o bloco retangular paramétrico, editável pelo painel e pelas alças do plugin. Essas alças não são parâmetros nativos do editor de blocos e dependem da DLL carregada.
- Use **+ Coletor** para um trecho horizontal na implantação, informando comprimento, declividade e rugosidade.

## Verificações realizadas

- 20 verificações automatizadas: cálculo, multiplicidade dos pavimentos, cópia de dados, duplicidade, conflito de revisões, fronteiras de diâmetro, sistema Pluvial/Reuso e compatibilidade com registros anteriores.
- GstarCAD 2025: importação do bloco dinâmico com novo nome, alteração para 150 mm, aplicação dos layers Pluvial/Reuso, comprimento da calha por API, cópia entre bancos DWG e salvamento/reabertura com dados.
- Inspeção visual da janela própria.

O arquivo **verificacao-gstarcad.txt** contém o resultado da execução nativa. **Biblioteca-Pluvial.dwg** contém exemplos com dados fictícios; para novos projetos, insira elementos pelo painel.

## Limites desta versão

O tubo vertical usa a tabela de referência da planilha fornecida. Isso não substitui a verificação pelos ábacos da NBR 10844 considerando lâmina, comprimento e tipo de saída. A calha verifica seção uniforme por Manning; bocais, curvas e extravasamento precisam de verificação adicional. O coletor usa Manning com lâmina de 2/3 do diâmetro interno; conferir diâmetro interno comercial.

Contornos devem ser simples e sem sobreposições; os acréscimos de contribuição são informados pelo projetista. A atualização de áreas alteradas exige remover o registro anterior e adicioná-lo novamente. O arraste interativo das alças e o fluxo completo de seleção manual ainda precisam de testes adicionais.

## Compilar e testar

Requisitos: Windows, GstarCAD 2025 e compilador do .NET Framework disponível no Windows. Execute `build.ps1` em PowerShell. Se o GstarCAD estiver em outra pasta, informe `-cad "caminho da instalação"`. O script aceita os fontes na raiz ou em `src/` e grava o plugin em `dist/`.

Os assemblies do GstarCAD são referenciados da instalação e não são distribuídos no repositório. A planilha original, projetos do usuário e credenciais não fazem parte desta publicação.

Fontes: `Core.cs` (modelo e cálculo), `Plugin.cs` (GstarCAD e dados DWG), `Panel.cs` (interface), `CoreTests.cs` (testes). A pasta `src/` no ambiente de desenvolvimento contém os mesmos fontes publicados na raiz deste repositório.
