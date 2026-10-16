# Duall Pluvial 1.5 — pacote específico GstarCAD 2023

Esta DLL foi adaptada e compilada para a API GrxCAD do GstarCAD 2023 (gmap/gmdb), diferente da API Gssoft usada no pacote 2025. Destina-se ao GstarCAD 2023 Windows 64 bits.

## No computador com GstarCAD 2023
1. Salve seus desenhos e reinicie o GstarCAD.
2. Extraia TODO o ZIP em uma pasta local permanente. Não abra a DLL diretamente dentro do ZIP.
3. Mantenha D-Prumada-Plu.dwg na mesma pasta de Duall.Pluvial.1.5.GC2023.dll.
4. Execute NETLOAD e selecione Duall.Pluvial.1.5.GC2023.dll (não use a DLL do 2025).
5. Execute PLUVIAL. O título deve indicar GstarCAD 2023.
6. Em um desenho de teste, confira inserir prumada, adicionar duas áreas, calcular sacada, unir prumadas e salvar/reabrir.

Não copie gmap.dll, gmdb.dll ou DLLs do GstarCAD 2025 para a instalação: as bibliotecas de execução pertencem ao CAD instalado.

Se ainda não carregar, abra o histórico de comandos com F2 após NETLOAD e envie o texto ou uma captura, junto da edição e versão completa mostradas em Sobre do GstarCAD. Caso o Windows indique bloqueio do arquivo baixado, confira Propriedades > Desbloquear no ZIP antes de extrair. Não é necessário desativar globalmente a segurança do CAD.

## Validação
Compilação específica para 2023 aprovada e 72 testes automáticos do cálculo/armazenamento aprovados. Compilação de regressão para 2025 também aprovada em arquivo separado. Ainda não foi validado o carregamento ou a interação dentro do GstarCAD 2023: a instalação utilizável está no outro computador do usuário.

As regras da versão 1.5 foram mantidas: teto de 200 mm, catálogo por linha, escolha manual, sacada = Length × altura / 8, contribuições e pavimentos tipo. Consulte LEIA-ME-1.5.md para esses detalhes; para instalação, prevalecem as instruções deste arquivo.

## Compilar
build.ps1 -target 2023 -cad "pasta contendo gmap.dll e gmdb.dll"
build.ps1 -target 2025 -cad "pasta da instalação 2025"

Referências de compilação 2023: pacote GstarCADNET 23.0.0 do NuGet (https://www.nuget.org/packages/GstarCADNET/23.0.0). Essas referências não são distribuídas no ZIP do plugin.
