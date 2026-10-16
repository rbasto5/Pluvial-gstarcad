using System;
using System.Linq;
using Duall.Pluvial;
class Tests {
 static int count;
 static void Assert(bool ok,string name){count++;if(!ok)throw new Exception(name);}
 static Element Base(){return new Element {Project="Obra A",City="São Paulo/SP",RainSource="Teste",Rain=180};}
 static Contribution C(string origin,string floor,double area){return new Contribution {Origin=origin,Floor=floor,Area=area,Kind="Sacada",Name="Sacada"};}
 static void Main(){
  var e=Base();foreach(int f in Enumerable.Range(1,12))e.Contributions.Add(C("sacada","P"+f,8));e.Contributions.Add(C("telhado","COB",50));
  Assert(e.Area==146,"Pavimento tipo não multiplica o telhado");Assert(e.Flow==438,"Vazão total");
  var copy=Model.Deserialize(Model.Serialize(e));Assert(copy.Area==146 && copy.Contributions[0].Origin=="sacada","Transferência completa JSON");
  Assert(Model.Merge(e,copy)==13 && e.Area==146,"Cópia não duplica contribuição");
  copy.Contributions[0].Area=9;bool rejected=false;try{Model.Merge(e,copy);}catch(Exception){rejected=true;}Assert(rejected && e.Area==146,"Conflito rejeitado atomicamente");
  var next=Base();next.Contributions.Add(C("sacada","P13",8));Model.Merge(e,next);Assert(e.Area==154,"Pavimento novo é somado");
  next.Rain=200;rejected=false;try{Model.Merge(e,next);}catch(Exception){rejected=true;}Assert(rejected,"Chuva incompatível rejeitada");
  Assert(Model.ReferenceDiameter(1.581051930980507*60)==100,"Limite inferior 100");Assert(Model.ReferenceDiameter((1.581051930980507-1e-9)*60)==75,"Imediatamente abaixo do limite");
  Assert(Math.Abs(Model.Capacity(Base())-1771.7062047)<.00001,"Manning comparado ao caso da planilha");
  Assert(Model.Floors("P01, P02; P03").Length==3,"Lista de pavimentos");rejected=false;try{Model.Floors("P01,p01");}catch(Exception){rejected=true;}Assert(rejected,"Pavimentos repetidos rejeitados");
  Assert(Math.Abs(Model.PipeCapacity(100,.5,.011)-204)<2,"Capacidade horizontal comparada com tabela 4 da NBR");
  var collector=Base();collector.Contributions.Add(C("a","P1",100));Assert(Model.CollectorDiameter(collector)==125,"Coletor escolhe capacidade suficiente");
  Assert(Model.RiserLayer("Pluvial")=="D-HID-PLU-TET","Layer Pluvial");
  Assert(Model.RiserLayer("Reuso")=="D-HID-APV-TET","Layer Reuso");
  var reuse=Model.Copy(e);reuse.Service="Reuso";Model.Validate(reuse);
  Assert(reuse.Flow==e.Flow && Model.ReferenceDiameter(reuse.Flow)==Model.ReferenceDiameter(e.Flow) && Model.Capacity(reuse)==Model.Capacity(e),"Sistema não altera cálculo");
  Assert(Model.Copy(reuse).Service=="Reuso","Sistema preservado no armazenamento e cópia");
  string legacy=System.Text.RegularExpressions.Regex.Replace(Model.Serialize(e),@",?""Service"":""Pluvial"",?",m=>m.Value.StartsWith(",") && m.Value.EndsWith(",")?",":"");
  Assert(Model.Deserialize(legacy).Service=="Pluvial","Compatibilidade de registros sem sistema");
  rejected=false;try{Model.RiserLayer("Outro");}catch(Exception){rejected=true;}Assert(rejected,"Sistema inválido não recebe layer arbitrário");
  var many=Base();var first=C("copied-guid","COB",40);first.Name="A1 [2F4]";first.Drawing="cobertura.dwg";
  var second=C("copied-guid","COB",60);second.Name="A2 [2F5]";second.Drawing="cobertura.dwg";
  Assert(Model.AddContribution(many,first) && Model.AddContribution(many,second) && many.Area==100,"Contornos copiados com áreas distintas são somados");
  Assert(!Model.AddContribution(many,first) && many.Contributions.Count==2,"Repetir seleção não duplica");
  var equal=C("copied-guid","COB",40);equal.Name="A3 [2F6]";equal.Drawing="cobertura.dwg";
  Assert(Model.AddContribution(many,equal) && many.Area==140,"Contornos distintos de mesma área também somam");
  var a=Base();a.Contributions.Add(first);var b=Base();b.Contributions.Add(second);
  Assert(Model.Merge(a,b)==0 && a.Area==100,"Unir prumadas com identificador legado clonado e contornos distintos");
  Assert(Model.Merge(a,b)==1 && a.Area==100,"Repetir união não duplica");
  var migrated=Model.Copy(b);migrated.Contributions[0].Source=Model.LegacySource(second.Name,second.Drawing);migrated.Contributions[0].Drawing="implantacao.dwg";
  Assert(Model.Merge(a,migrated)==1 && a.Area==100,"Procedência preservada ao copiar para outro arquivo");
  var conflict=Model.Copy(b);conflict.Contributions.Insert(0,C("new","P1",5));conflict.Contributions[1].Area=61;
  rejected=false;try{Model.Merge(a,conflict);}catch(Exception){rejected=true;}
  Assert(rejected && a.Area==100 && a.Contributions.Count==2,"Revisão verdadeira rejeitada sem união parcial");
  var bulk=Base();foreach(int n in Enumerable.Range(1,1000))Model.AddContribution(bulk,C("area"+n,"P01",1));
  var restored=Model.Copy(bulk);Model.Validate(restored);
  Assert(restored.Contributions.Count==1000 && restored.Area==1000,"Mil registros preservados, sem limite de uma linha");
  foreach(var floor in Model.Floors("P01,P02,P03"))Model.AddContribution(many,C("tipo",floor,10));
  Assert(many.Area==170 && many.Contributions.Count==6,"Repetição por pavimento soma apenas a área tipo");
  var renamed=Model.Copy(migrated);renamed.Contributions[0].Name="Novo nome [2F5]";
  Assert(renamed.Contributions[0].Key==migrated.Contributions[0].Key,"Nome não altera procedência persistida");
  Assert(Model.BalconyArea(4,3)==1.5,"Sacada 4 x 3 / 8 = 1,5 m²");
  var balconies=Base();foreach(var floor in Model.Floors("P01,P02,P03")) {
   var balcony=C("balcony",floor,Model.BalconyArea(4,3));balcony.Formula=Model.BalconyFormula;balcony.BalconyWidth=4;balcony.BalconyHeight=3;Model.AddContribution(balconies,balcony);
  }
  Model.Validate(balconies);
  Assert(balconies.Area==4.5 && balconies.Flow==13.5,"Sacada tipo usa área calculada por pavimento");
  var roof=C("roof","COB",40);roof.Kind="Telhado";Model.AddContribution(balconies,roof);
  Assert(balconies.Area==44.5 && balconies.Flow==133.5,"Telhado mantém área do contorno ao somar sacadas");
  var balconyCopy=Model.Copy(balconies);
  Assert(balconyCopy.Contributions[0].BalconyWidth==4 && balconyCopy.Contributions[0].BalconyHeight==3 && balconyCopy.Contributions[0].Formula==Model.BalconyFormula,"Dimensões e fórmula sobrevivem à serialização");
  Assert(Model.Merge(balconies,balconyCopy)==4 && balconies.Area==44.5,"União de sacadas preserva deduplicação");
  balconyCopy.Contributions[0].BalconyWidth=6;balconyCopy.Contributions[0].BalconyHeight=2;
  rejected=false;try{Model.Merge(balconies,balconyCopy);}catch(Exception){rejected=true;}Assert(rejected && balconies.Area==44.5,"Dimensões diferentes com mesma área são revisões conflitantes");
  foreach(double invalid in new[]{0.0,-1.0,Double.NaN,Double.PositiveInfinity}) {
   rejected=false;try{Model.BalconyArea(invalid,3);}catch(Exception){rejected=true;}Assert(rejected,"Largura inválida rejeitada");
   rejected=false;try{Model.BalconyArea(4,invalid);}catch(Exception){rejected=true;}Assert(rejected,"Altura inválida rejeitada");
  }
  balconyCopy=Model.Copy(balconies);balconyCopy.Contributions[0].Area=12;
  rejected=false;try{Model.Validate(balconyCopy);}catch(Exception){rejected=true;}Assert(rejected,"Área incompatível com dimensões rejeitada");
  var oldBalcony=Base();oldBalcony.Contributions.Add(C("old","P1",12));Model.Validate(Model.Copy(oldBalcony));
  Assert(Model.Copy(oldBalcony).Area==12 && Model.Copy(oldBalcony).Contributions[0].Formula==null,"Sacadas anteriores preservadas sem inventar altura");
  balconyCopy=Model.Copy(balconies);balconyCopy.Contributions[0].Extra=2;Model.Validate(balconyCopy);
  Assert(balconyCopy.Area==46.5,"Acréscimo manual continua somado após a fórmula");
  var sizes=new double[]{20,25,32,40,50,65,75,90,100,110,150,200};
  var sizing=Base();sizing.Contributions.Add(C("size","P1",80));
  Model.ResolveRiserDiameter(sizing,sizes,100);
  Assert(sizing.SuggestedDiameter==150 && sizing.Diameter==150,"Automático adota sugestão de 150 mm");
  sizing.ManualDiameter=200;Model.ResolveRiserDiameter(sizing,sizes,150);
  Assert(sizing.SuggestedDiameter==150 && sizing.Diameter==200,"Aumento manual mantém sugestão");
  sizing.ManualDiameter=100;Model.ResolveRiserDiameter(sizing,sizes,200);
  Assert(sizing.SuggestedDiameter==150 && sizing.Diameter==100 && sizing.Status.Contains("abaixo do sugerido"),"Redução manual permitida com aviso");
  var sizedCopy=Model.Copy(sizing);
  Assert(sizedCopy.ManualDiameter==100 && sizedCopy.SuggestedDiameter==150 && sizedCopy.Diameter==100,"Sugestão e escolha manual preservadas no bloco");
  sizing.Contributions.Add(C("more","P1",150));Model.ResolveRiserDiameter(sizing,sizes,100);
  Assert(sizing.SuggestedDiameter==200 && sizing.Diameter==100,"Nova contribuição recalcula sugestão sem perder escolha manual");
  sizing.ManualDiameter=0;Model.ResolveRiserDiameter(sizing,sizes,100);
  Assert(sizing.Diameter==200,"Zero retorna ao automático");
  sizing.ManualDiameter=125;rejected=false;try{Model.ResolveRiserDiameter(sizing,sizes,200);}catch(Exception){rejected=true;}
  Assert(rejected,"Diâmetro ausente no bloco rejeitado");
  sizing.ManualDiameter=Double.NaN;rejected=false;try{Model.Validate(sizing);}catch(Exception){rejected=true;}Assert(rejected,"Diâmetro manual inválido rejeitado");
  sizing.ManualDiameter=0;sizing.Contributions[0].Area=1000;Model.ResolveRiserDiameter(sizing,sizes,200);
  Assert(sizing.SuggestedDiameter==0 && sizing.Diameter==200 && sizing.Status.Contains("mais de 200"),"Sugestão maior que bloco mantém gráfico e avisa");
  sizing.Contributions[0].Area=100000;Model.ResolveRiserDiameter(sizing,sizes,200);
  Assert(sizing.SuggestedDiameter==0 && sizing.Diameter==200 && sizing.Status.Contains("fora da tabela"),"Fora da tabela não mantém sugestão antiga");
  sizing.ManualDiameter=150;Model.ResolveRiserDiameter(sizing,sizes,200);
  Assert(sizing.SuggestedDiameter==0 && sizing.Diameter==150,"Manual disponível mesmo sem sugestão calculável");
  var emptySizing=Base();Model.ResolveRiserDiameter(emptySizing,sizes,100);
  Assert(emptySizing.SuggestedDiameter==0 && emptySizing.Diameter==100,"Sem áreas mostra diâmetro gráfico sem inventar sugestão");
  Assert(Model.Copy(oldBalcony).ManualDiameter==0,"Registros antigos continuam automáticos");
  var capped=Base();capped.Contributions.Add(C("cap","P1",6000));
  Model.ResolveRiserDiameter(capped,new double[]{100,150,200,250,300,400,500,600},200);
  Assert(capped.SuggestedDiameter==0 && capped.Diameter==200 && capped.Status.Contains("mais de 200"),"Demanda acima de 200 não gera sugestão proibida");
  capped.ManualDiameter=600;rejected=false;try{Model.Validate(capped);}catch(Exception){rejected=true;}Assert(rejected,"Limite de 200 também vale para manual");
  rejected=false;try{Model.ResolveRiserDiameter(capped,new double[]{500,600},200);}catch(Exception){rejected=true;}Assert(rejected,"Bloco com opção 600 não contorna limite");
  capped.ManualDiameter=200;Model.ResolveRiserDiameter(capped,new double[]{200,500,600},200);
  Assert(capped.Diameter==200 && capped.SuggestedDiameter==0,"200 aceito como teto sem sugerir capacidade suficiente");
  var commercial=Base();commercial.Contributions.Add(C("catalog","P1",1));
  Model.ResolveRiserDiameter(commercial,sizes,75);
  Assert(commercial.SuggestedDiameter==100 && commercial.Diameter==100,"Sugestão prioriza DN100 mesmo quando referência é DN75");
  commercial.Contributions[0].Area=300;commercial.PipeLine="Amanco PVC reforçado";
  Model.ResolveRiserDiameter(commercial,new double[]{150,200,300},150);
  Assert(commercial.SuggestedDiameter==0,"Reforçado não sugere DN200 ausente no catálogo");
  commercial.PipeLine="Amanco PVC branco";commercial.Contributions[0].Area=80;
  Model.ResolveRiserDiameter(commercial,new double[]{100,200},100);
  Assert(commercial.SuggestedDiameter==150 && commercial.Diameter==100 && commercial.Status.Contains("indisponível no bloco"),"Sugestão comercial não falsifica medida ausente no bloco");
  commercial.ManualDiameter=250;rejected=false;try{Model.ResolveRiserDiameter(commercial,new double[]{200,250},200);}catch(Exception){rejected=true;}Assert(rejected,"DN250 bloqueado mesmo presente no bloco");
  commercial.ManualDiameter=110;rejected=false;try{Model.ResolveRiserDiameter(commercial,sizes,100);}catch(Exception){rejected=true;}Assert(rejected,"Bitola DN110 do bloco não pertence ao catálogo predial cadastrado");
  commercial.ManualDiameter=75;Model.ResolveRiserDiameter(commercial,sizes,100);
  Assert(commercial.Diameter==75 && commercial.Status.Contains("abaixo do sugerido"),"Redução manual comercial permitida com aviso");
  commercial.PipeLine="Amanco PVC reforçado";
  Assert(Model.Copy(commercial).PipeLine=="Amanco PVC reforçado","Linha preservada na cópia");
  var legacyLine=Model.Serialize(Base()).Replace(",\"PipeLine\":\"Amanco PVC branco\"","");
  Assert(Model.Deserialize(legacyLine).PipeLine=="Amanco PVC branco","Registro sem linha recebe padrão compatível");
  Console.WriteLine(count+" verificações aprovadas.");
 }
}


