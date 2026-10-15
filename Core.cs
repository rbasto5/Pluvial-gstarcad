using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;

namespace Duall.Pluvial {
 [DataContract] public class Contribution {
  [DataMember] public string Origin;
  [DataMember] public string Name;
  [DataMember] public string Kind;
  [DataMember] public string Floor;
  [DataMember] public double Area;
  [DataMember] public double Extra;
  [DataMember] public string Drawing;
  public string Key { get { return Origin + "|" + Floor.ToUpperInvariant(); } }
  public double Effective { get { return Area + Extra; } }
 }
 [DataContract] public class Element {
  [DataMember] public int Version = 1;
  [DataMember] public string Id = Guid.NewGuid().ToString("N");
  [DataMember] public string Project = "";
  [DataMember] public string Name = "PLU-01";
  [DataMember] public string Kind = "Prumada";
  [DataMember] public string Service = "Pluvial";
  [DataMember] public string City = "";
  [DataMember] public string RainSource = "";
  [DataMember] public double Rain = 0;
  [DataMember] public int ReturnYears = 5;
  [DataMember] public double Duration = 5;
  [DataMember] public double Length = 3;
  [DataMember] public double Width = .3;
  [DataMember] public double Water = .1;
  [DataMember] public double Height = .15;
  [DataMember] public double Slope = .5;
  [DataMember] public double Roughness = .011;
  [DataMember] public double Diameter;
  [DataMember] public string Status = "Sem contribuicoes";
  [DataMember] public string Updated;
  [DataMember] public List<Contribution> Contributions = new List<Contribution>();
  public double Area { get { return Contributions.Sum(x => x.Effective); } }
  public double Flow { get { return Rain * Area / 60; } }
 }
 public static class Model {
  public static string Serialize(Element e) {
   using(var m = new MemoryStream()) { new DataContractJsonSerializer(typeof(Element)).WriteObject(m,e); return Encoding.UTF8.GetString(m.ToArray()); }
  }
  public static Element Deserialize(string s) {
   using(var m = new MemoryStream(Encoding.UTF8.GetBytes(s))) {
    var e=(Element)new DataContractJsonSerializer(typeof(Element)).ReadObject(m);
    if(e == null || e.Version != 1 || e.Contributions == null || String.IsNullOrEmpty(e.Id)) throw new Exception("Registro Pluvial incompatível ou incompleto.");
    if(String.IsNullOrEmpty(e.Service)) e.Service="Pluvial";
    return e;
   }
  }
  public static Element Copy(Element e) { return Deserialize(Serialize(e)); }
  public static string RiserLayer(string service) {
   if(service=="Pluvial")return "D-HID-PLU-TET";
   if(service=="Reuso")return "D-HID-APV-TET";
   throw new Exception("Escolha Pluvial ou Reuso.");
  }
  public static void Validate(Element e) {
   RiserLayer(e.Service);
   if(String.IsNullOrWhiteSpace(e.Project) || String.IsNullOrWhiteSpace(e.City) || String.IsNullOrWhiteSpace(e.RainSource)) throw new Exception("Preencha projeto, município e fonte da chuva.");
   if(!(e.Rain > 0) || Double.IsInfinity(e.Rain) || e.ReturnYears < 1 || !(e.Duration>0)) throw new Exception("Informe chuva, TR e duração positivos.");
   if(!(e.Length>0 && e.Width>0 && e.Water>0 && e.Height>e.Water && e.Slope>0 && e.Roughness>0)) throw new Exception("Dimensões, declividade e rugosidade devem ser positivas. Altura total deve superar a lâmina de água.");
   var keys = new HashSet<string>();
   foreach(var c in e.Contributions) {
    if(String.IsNullOrWhiteSpace(c.Origin) || String.IsNullOrWhiteSpace(c.Floor) || !(c.Area>0) || c.Extra<0 || Double.IsInfinity(c.Effective) || Double.IsNaN(c.Extra)) throw new Exception("Contribuição inválida.");
    if(!keys.Add(c.Key)) throw new Exception("Contribuição duplicada para o mesmo pavimento.");
   }
  }
  public static double Capacity(Element e) {
   double a=e.Width*e.Water, r=a/(e.Width+2*e.Water);
   return 60000*a*Math.Pow(r,.667)*Math.Sqrt(e.Slope/100)/e.Roughness;
  }
  public static double PipeCapacity(double diameterMm,double slopePercent,double roughness) {
   if(diameterMm<=0 || slopePercent<=0 || roughness<=0) throw new Exception("Dados inválidos para o coletor.");
   double radius=diameterMm/2000.0, angle=2*Math.Acos(-1.0/3.0);
   double area=radius*radius*(angle-Math.Sin(angle))/2, perimeter=radius*angle;
   return 60000*area*Math.Pow(area/perimeter,2.0/3.0)*Math.Sqrt(slopePercent/100)/roughness;
  }
  public static double CollectorDiameter(Element e) {
   foreach(int d in new[]{75,100,125,150,200,250,300,400,500,600,700,800,900,1000})
    if(PipeCapacity(d,e.Slope,e.Roughness)>=e.Flow)return d;
   return 0;
  }
  public static double ReferenceDiameter(double litresPerMinute) {
   double[] starts={.001,1.581051930980507,3.404164729326052,10.036022704343623,21.614742343402565,39.19223729225928,63.734146172101596,137.2714218697947,248.90734815723175,404.7747649099969,610.6047177511249,871.8152569171109,1193.5705871527416};
   int[] diameters={75,100,150,200,250,300,400,500,600,700,800,900,1000};
   double q=litresPerMinute/60;
   if(q<starts[0] || q>=1580.823000871245) throw new Exception("Vazão fora da tabela de referência da planilha.");
   int idx=0; for(int k=0;k<starts.Length;k++) if(q>=starts[k]) idx=k;
   return diameters[idx];
  }
  public static int Merge(Element destination, Element source) {
   Validate(destination); Validate(source);
   if(destination.Project.Trim()!=source.Project.Trim() || destination.City.Trim()!=source.City.Trim() || Math.Abs(destination.Rain-source.Rain)>1e-8 || destination.ReturnYears!=source.ReturnYears || Math.Abs(destination.Duration-source.Duration)>1e-8 || destination.RainSource.Trim()!=source.RainSource.Trim())
    throw new Exception("Projeto ou critérios de chuva diferentes. Compatibilize os dados antes de unir.");
   var existing=destination.Contributions.ToDictionary(c=>c.Key);
   var additions=new List<Contribution>(); int duplicates=0;
   foreach(var c in source.Contributions) {
    Contribution old;
    if(existing.TryGetValue(c.Key,out old)) {
     if(Math.Abs(old.Area-c.Area)>1e-8 || Math.Abs(old.Extra-c.Extra)>1e-8 || old.Kind!=c.Kind) throw new Exception("A origem "+c.Name+" / "+c.Floor+" possui revisões diferentes. Resolva a contribuição antes de unir.");
     duplicates++;
    } else { additions.Add(c); existing.Add(c.Key,c); }
   }
   destination.Contributions.AddRange(additions); return duplicates;
  }
  public static string[] Floors(string text) {
   var floors=text.Split(new[]{',',';'},StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Trim().ToUpperInvariant()).Where(s=>s.Length>0).ToArray();
   if(floors.Length==0 || floors.Length!=floors.Distinct().Count()) throw new Exception("Informe pavimentos distintos separados por vírgula.");
   return floors;
  }
 }
}
