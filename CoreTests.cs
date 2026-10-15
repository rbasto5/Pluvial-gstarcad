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
  Console.WriteLine(count+" verificações aprovadas.");
 }
}
