# Duall Pluvial e Reuso — versão 1.5

## Instalar
Salve os desenhos e reinicie o GstarCAD 2025. Extraia o pacote mantendo D-Prumada-Plu.dwg junto de Duall.Pluvial.1.5.dll. Carregue a DLL com NETLOAD e abra PLUVIAL.

## Limite e bitolas
O limite de prumadas agora é 200 mm, tanto para sugestão quanto para escolha manual. A sugestão automática começa em 100 mm e usa a menor bitola cadastrada suficiente. Se a referência exigir mais de 200 mm, o painel não apresenta diâmetro sugerido válido: informa que é necessário redistribuir contribuições ou revisar a solução. O gráfico existente é preservado; isso não indica atendimento à vazão.

Linha de tubos, na aba Projeto / elemento:
- Amanco PVC branco: catálogo DN40, 50, 75, 100, 150 e 200. Sugestões automáticas: 100, 150 ou 200 mm.
- Amanco PVC reforçado: catálogo DN40, 50, 75, 100 e 150. Sugestões automáticas: 100 ou 150 mm. DN200 não foi cadastrado nesta linha porque não consta da ficha consultada.

Novafort e bitolas acima de 200 mm ficaram fora desta versão conforme o novo escopo. Diâmetros existentes no bloco, como 90 e 110 mm, não são aceitos como DN dessas linhas. Registros anteriores sem linha recebem o padrão PVC branco; confira o material e salve/recalcule.

O campo Diâmetro manual continua permitindo ajustes compatíveis com a linha e o bloco. Zero retorna ao automático. Reduções abaixo do sugerido exibem aviso. A escolha manual do destino é preservada ao adicionar contribuições ou unir prumadas. Linha, sugestão e diâmetro adotado acompanham os registros no bloco e nas exportações.

## Teste curto
Use chuva fictícia de 180 mm/h e acréscimo zero:
1. PVC branco, área 80 m²: sugerido 150 mm. Área total 230 m²: sugerido 200 mm.
2. Altere para PVC reforçado com 230 m²: não deve sugerir 200 mm; deve avisar que a linha não possui medida suficiente.
3. PVC branco, área 500 m²: não deve sugerir 250 mm nem considerar 200 mm suficiente; deve avisar que excedeu o limite.
4. Manual 250 mm deve ser recusado. Manual 200 mm é permitido no PVC branco quando disponível no bloco.
5. Com área 80 m², manual 100 mm deve manter sugerido 150 mm e indicar adotado abaixo do sugerido.

## Funcionalidades preservadas
Sacadas usam Length total da polyline × altura manual / 8. Telhados e implantação usam área de contorno fechado. Pavimentos tipo, várias contribuições, união sem duplicação e cópia de dados entre DWGs permanecem. Pluvial usa D-HID-PLU-TET; Reuso usa D-HID-APV-TET. Calhas e coletores continuam em standby.

## Validação e fontes
Compilação aprovada com assemblies do GstarCAD 2025; 72 verificações automáticas locais aprovadas. A interação da versão 1.5 ainda depende de teste no GstarCAD.

Catálogos consultados em 15/10/2026:
- PVC branco: https://mediahub.wavin.com/m/7995c83fae479437/original/FT-Esgoto-SN.pdf
- PVC reforçado: https://mediahub.wavin.com/m/1eb846476e17e767/original/FT-Linha-Esgoto-SR.pdf

As bitolas cadastradas são nominais comerciais. O cálculo mantém a tabela de referência da planilha; esta atualização não substitui a verificação vertical pelos ábacos da NBR 10844 nem a conferência de diâmetro interno e capacidade da linha especificada.
