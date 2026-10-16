using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
#if GC2023
using GrxCAD.Runtime;
using GrxCAD.ApplicationServices;
using GrxCAD.DatabaseServices;
using GrxCAD.EditorInput;
using GrxCAD.Geometry;
using App = GrxCAD.ApplicationServices.Application;
#else
using Gssoft.Gscad.Runtime;
using Gssoft.Gscad.ApplicationServices;
using Gssoft.Gscad.DatabaseServices;
using Gssoft.Gscad.EditorInput;
using Gssoft.Gscad.Geometry;
using App = Gssoft.Gscad.ApplicationServices.Application;
using CadException = Gssoft.Gscad.Runtime.Exception;
#endif

[assembly: CommandClass(typeof(Duall.Pluvial.Plugin))]
[assembly: ExtensionApplication(typeof(Duall.Pluvial.Plugin))]
namespace Duall.Pluvial {
 public class Plugin : IExtensionApplication {
#if GC2023
  internal const string CadVersion="2023";
#else
  internal const string CadVersion="2025";
#endif
  internal static Panel Window;
  internal static ObjectId Selected=ObjectId.Null;
  internal static Database SelectedDb;
  const string Key="DUALL_PLUVIAL_V1";
  const string RiserBlockName="D-Prumada-Plu";
  const string RiserTemplate="D-Prumada-Plu.dwg";
  
  public void Initialize() {
   foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
    if(assembly==typeof(Plugin).Assembly || !assembly.GetName().Name.StartsWith("Duall.Pluvial"))continue;
    var old=assembly.GetType("Duall.Pluvial.Plugin");if(old!=null)old.GetMethod("Terminate").Invoke(Activator.CreateInstance(old),null);
   }
   App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\nDuall Pluvial 1.5 — Prumadas. Digite PLUVIAL para abrir o painel.\n"); }
  public void Terminate() { if(Window!=null) Window.Dispose(); }
  [CommandMethod("PLUDESATIVAR")]
  public void Disable() { Terminate(); }
  [CommandMethod("PLUVIAL")]
  public void Open() { if(Window==null || Window.IsDisposed) Window=new Panel(); App.ShowModelessDialog(Window); }
  internal static string Folder { get { return Path.GetDirectoryName(typeof(Plugin).Assembly.Location); } }
  internal static void Request(string command) {
   var doc=App.DocumentManager.MdiActiveDocument;
   if(doc==null) return;
   doc.SendStringToExecute(command+" ",true,false,false);
  }
  static void Run(Action<Document> action) {
   if(Window==null || Window.IsDisposed) Window=new Panel();
   Window.Hide();
   try { var doc=App.DocumentManager.MdiActiveDocument; if(doc==null) throw new System.Exception("Abra um desenho."); using(doc.LockDocument()) action(doc); }
   catch(System.Exception e) { System.Windows.Forms.MessageBox.Show(e.Message,"Pluvial",System.Windows.Forms.MessageBoxButtons.OK,System.Windows.Forms.MessageBoxIcon.Information); }
   finally { Window.Show(); }
  }
  internal static string ReadText(DBObject obj,Transaction tr,string key) {
   if(obj.ExtensionDictionary.IsNull) return null;
   var dict=(DBDictionary)tr.GetObject(obj.ExtensionDictionary,OpenMode.ForRead);
   if(!dict.Contains(key)) return null;
   var x=(Xrecord)tr.GetObject(dict.GetAt(key),OpenMode.ForRead);
   using(var data=x.Data) { return data==null?null:String.Concat(data.AsArray().Select(v=>Convert.ToString(v.Value))); }
  }
  internal static void WriteText(DBObject obj,Transaction tr,string key,string text) {
   if(!obj.IsWriteEnabled) obj.UpgradeOpen();
   if(obj.ExtensionDictionary.IsNull) obj.CreateExtensionDictionary();
   var dict=(DBDictionary)tr.GetObject(obj.ExtensionDictionary,OpenMode.ForWrite);
   Xrecord x;
   if(dict.Contains(key)) x=(Xrecord)tr.GetObject(dict.GetAt(key),OpenMode.ForWrite);
   else { x=new Xrecord(); dict.SetAt(key,x); tr.AddNewlyCreatedDBObject(x,true); }
   var values=new List<TypedValue>();
   for(int i=0;i<text.Length;i+=200) values.Add(new TypedValue((int)DxfCode.Text,text.Substring(i,Math.Min(200,text.Length-i))));
   using(var data=new ResultBuffer(values.ToArray())) x.Data=data;
  }
  internal static Element Read(BlockReference b,Transaction tr) { var s=ReadText(b,tr,Key); return s==null?null:Model.Deserialize(s); }
  internal static void Save(BlockReference b,Transaction tr,Element e) { e.Updated=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); WriteText(b,tr,Key,Model.Serialize(e)); }
  static ObjectId PickBlock(Document doc,string msg) {
   var opt=new PromptEntityOptions("\n"+msg); opt.SetRejectMessage("\nSelecione um bloco."); opt.AddAllowedClass(typeof(BlockReference),true);
   var r=doc.Editor.GetEntity(opt); return r.Status==PromptStatus.OK?r.ObjectId:ObjectId.Null;
  }
  static void CheckSelected(Document doc) { if(Selected.IsNull || SelectedDb!=doc.Database || !Selected.IsValid || Selected.IsErased) throw new System.Exception("Clique em Selecionar elemento neste desenho."); }
  static PromptSelectionResult SelectMany(Document doc,PromptSelectionOptions options,SelectionFilter filter) {
   var previous=App.GetSystemVariable("PICKADD");
   try {App.SetSystemVariable("PICKADD",2);options.SingleOnly=false;return doc.Editor.GetSelection(options,filter);}
   finally {App.SetSystemVariable("PICKADD",previous);}
  }
  static void Meters(Document doc) { if(doc.Database.Insunits!=UnitsValue.Meters && doc.Database.Insunits!=UnitsValue.Undefined) throw new System.Exception("O desenho deve estar em metros. Confira UNITS/INSUNITS."); if(!Window.MetersConfirmed) throw new System.Exception("Confirme no painel que as medidas do desenho estão em metros."); }
  static string BlockName(BlockReference b,Transaction tr) { return ((BlockTableRecord)tr.GetObject(b.IsDynamicBlock?b.DynamicBlockTableRecord:b.BlockTableRecord,OpenMode.ForRead)).Name; }
  static void ApplyRiserLayer(BlockReference b,Transaction tr,Element e) {
   var name=Model.RiserLayer(e.Service);
   var layers=(LayerTable)tr.GetObject(b.Database.LayerTableId,OpenMode.ForRead);
   ObjectId id;
   if(layers.Has(name))id=layers[name];
   else {layers.UpgradeOpen();var layer=new LayerTableRecord {Name=name};id=layers.Add(layer);tr.AddNewlyCreatedDBObject(layer,true);}
   b.LayerId=id;
  }
  internal static void Update(BlockReference b,Transaction tr,Element e) {
   if(e.Kind!="Prumada")throw new System.Exception("Calhas e coletores estão em standby nesta versão.");
   Model.Validate(e); e.Status="Sem contribuições";
   if(e.Kind=="Calha") {
    if(BlockName(b,tr)!="DUALL-CALHA") throw new System.Exception("Escolha o bloco DUALL-CALHA para este tipo de elemento.");
    b.ScaleFactors=new Scale3d(e.Length,e.Width,1);
    e.Status=e.Contributions.Count==0?"Sem contribuições":(Model.Capacity(e)>=e.Flow?"Seção atende à vazão uniforme":"CAPACIDADE INSUFICIENTE");
    e.Status+="; capacidade "+Model.Capacity(e).ToString("F2")+" L/min";
    if(e.Slope<.5) e.Status+="; declividade menor que 0,5%";
    e.Status+="; verificar curvas, saídas e bordo livre";
   } else if(e.Kind=="Coletor") {
    if(BlockName(b,tr)!="DUALL-COLETOR")throw new System.Exception("Escolha o bloco DUALL-COLETOR para o trecho horizontal.");
    if(e.Contributions.Count==0){e.Diameter=0;e.Status="Sem contribuições";return;}
    e.Diameter=Model.CollectorDiameter(e);
    if(e.Diameter==0){e.Status="Vazão acima das seções disponíveis; diâmetro gráfico mantido.";return;}
    b.ScaleFactors=new Scale3d(e.Length,e.Diameter/1000,1);
    e.Status="Diâmetro interno mínimo "+e.Diameter+" mm; Manning, lâmina 2/3 D";
    if(e.Slope<.5)e.Status+="; ATENÇÃO: declividade inferior a 0,5%";
    e.Status+="; conferir diâmetro interno comercial e conexões";
   } else {
    if(!b.IsDynamicBlock || !BlockName(b,tr).Equals(RiserBlockName,StringComparison.OrdinalIgnoreCase)) throw new System.Exception("Use o bloco D-Prumada-Plu inserido pelo painel. Os demais blocos de prumada não serão alterados.");
    DynamicBlockReferenceProperty property=null;
    foreach(DynamicBlockReferenceProperty p in b.DynamicBlockReferencePropertyCollection) if(p.PropertyName.Equals("PRUMADA",StringComparison.OrdinalIgnoreCase)) property=p;
    if(property==null || property.ReadOnly) throw new System.Exception("O bloco não possui PRUMADA editável.");
    ApplyRiserLayer(b,tr,e);
    var available=new SortedDictionary<double,object>();
    foreach(object value in property.GetAllowedValues()) {
     double mm;var digits=System.Text.RegularExpressions.Regex.Match(Convert.ToString(value),@"\d+(?:[.,]\d+)?").Value;
     if(Double.TryParse(digits.Replace(',','.'),NumberStyles.Any,CultureInfo.InvariantCulture,out mm))available[mm]=value;
    }
    double actual=0;var actualDigits=System.Text.RegularExpressions.Regex.Match(Convert.ToString(property.Value),@"\d+(?:[.,]\d+)?").Value;
    Double.TryParse(actualDigits.Replace(',','.'),NumberStyles.Any,CultureInfo.InvariantCulture,out actual);
    Model.ResolveRiserDiameter(e,available.Keys,actual);
    if(available.ContainsKey(e.Diameter))property.Value=available[e.Diameter];
   }
  }
  [CommandMethod("PLUSELECIONAR")]
  public void Select() { Run(doc=> { var id=PickBlock(doc,"Selecione calha ou prumada:"); if(id.IsNull)return;
   using(var tr=doc.Database.TransactionManager.StartTransaction()) {
    var b=(BlockReference)tr.GetObject(id,OpenMode.ForRead); var e=Read(b,tr);
    if(e==null) { e=Window.GetElement(); e.Id=Guid.NewGuid().ToString("N"); e.Contributions.Clear();var name=BlockName(b,tr);e.Kind=name=="DUALL-CALHA"?"Calha":(name=="DUALL-COLETOR"?"Coletor":"Prumada"); e.Status="Ainda não vinculado: preencha os dados e salve."; }
    if(e.Kind!="Prumada" || !BlockName(b,tr).Equals(RiserBlockName,StringComparison.OrdinalIgnoreCase))throw new System.Exception("Selecione uma D-Prumada-Plu. Calhas e coletores estão em standby.");
    Selected=id; SelectedDb=doc.Database; Window.LoadElement(e);
   }
  }); }
  [CommandMethod("PLUSALVAR")]
  public void Store() { Run(doc=> { Meters(doc); CheckSelected(doc); var e=Window.GetElement(); Model.Validate(e);
   using(var tr=doc.Database.TransactionManager.StartTransaction()) { var b=(BlockReference)tr.GetObject(Selected,OpenMode.ForWrite); Update(b,tr,e); Save(b,tr,e); tr.Commit(); }
   Window.LoadElement(e); doc.Editor.Regen();
  }); }
  [CommandMethod("PLUADICIONAR")]
  public void Add() { Run(doc=> { Meters(doc); CheckSelected(doc); var e=Window.GetElement(); Model.Validate(e);
   var settings=Window.GetContribution(); var floors=Model.Floors(settings.Floor);
   var opts=new PromptSelectionOptions(); opts.MessageForAdding=settings.Kind=="Sacada"?"\nSelecione polylines das sacadas (largura = Length total): ":"\nSelecione contornos fechados em projeção horizontal: ";
   var selection=SelectMany(doc,opts,new SelectionFilter(new[]{new TypedValue((int)DxfCode.Start,"LWPOLYLINE")}));
   if(selection.Status!=PromptStatus.OK)return;
   int duplicates=0;
   using(var tr=doc.Database.TransactionManager.StartTransaction()) {
    foreach(ObjectId id in selection.Value.GetObjectIds()) {
     var poly=(Polyline)tr.GetObject(id,OpenMode.ForRead);
     if(Math.Abs(poly.Normal.Z)<.999999)throw new System.Exception("Selecione polylines em projeção horizontal.");
     double contributionArea,balconyWidth=0;
     if(settings.Kind=="Sacada") {
      balconyWidth=poly.Length;
      contributionArea=Model.BalconyArea(balconyWidth,settings.BalconyHeight);
     } else {
      if(!poly.Closed || poly.Area<=0)throw new System.Exception("Telhados e implantação exigem contornos fechados com área positiva.");
      contributionArea=poly.Area;
     }
     string origin=ReadText(poly,tr,"DUALL_PLUVIAL_ORIGIN");
     string owner=Convert.ToString(doc.Database.FingerprintGuid)+"|"+poly.Handle.ToString();
     string previousOwner=ReadText(poly,tr,"DUALL_PLUVIAL_OWNER");
     string source=ReadText(poly,tr,"DUALL_PLUVIAL_SOURCE");
     if(origin==null || (previousOwner!=null && previousOwner!=owner)) {
      origin=Guid.NewGuid().ToString("N");source=null;
     }
     if(source==null)source=Model.LegacySource("["+poly.Handle.ToString()+"]",Path.GetFileName(doc.Name));
     WriteText(poly,tr,"DUALL_PLUVIAL_ORIGIN",origin);
     WriteText(poly,tr,"DUALL_PLUVIAL_OWNER",owner);
     WriteText(poly,tr,"DUALL_PLUVIAL_SOURCE",source);
     foreach(string floor in floors) {
      var c=new Contribution {Origin=origin,Source=source,Name=settings.Name+" ["+poly.Handle.ToString()+"]",Kind=settings.Kind,Floor=floor,Area=contributionArea,Extra=settings.Extra,Drawing=Path.GetFileName(doc.Name),Formula=settings.Formula,BalconyWidth=balconyWidth,BalconyHeight=settings.BalconyHeight};
      if(!Model.AddContribution(e,c))duplicates++;
     }
    }
    var b=(BlockReference)tr.GetObject(Selected,OpenMode.ForWrite); Update(b,tr,e); Save(b,tr,e); tr.Commit();
   }
   Window.LoadElement(e); doc.Editor.Regen(); Window.Notify(duplicates+" ocorrências já existentes foram ignoradas.");
  }); }
  [CommandMethod("PLUUNIR")]
  public void Merge() { Run(doc=> { CheckSelected(doc); var e=Window.GetElement(); Model.Validate(e);
   var options=new PromptSelectionOptions(); options.MessageForAdding="\nSelecione os blocos de origem para somar ao elemento do painel: ";
   var r=SelectMany(doc,options,new SelectionFilter(new[]{new TypedValue((int)DxfCode.Start,"INSERT")})); if(r.Status!=PromptStatus.OK)return;
   int duplicate=0;
   using(var tr=doc.Database.TransactionManager.StartTransaction()) {
    foreach(ObjectId id in r.Value.GetObjectIds()) {
     if(id==Selected) throw new System.Exception("Não inclua o próprio bloco de destino na seleção.");
     var source=Read((BlockReference)tr.GetObject(id,OpenMode.ForRead),tr);
     if(source==null) throw new System.Exception("Um dos blocos ainda não possui dados Pluvial.");
     if(source.Kind!="Prumada")throw new System.Exception("Selecione apenas prumadas para unir.");
     duplicate+=Model.Merge(e,source);
    }
    var b=(BlockReference)tr.GetObject(Selected,OpenMode.ForWrite); Update(b,tr,e); Save(b,tr,e); tr.Commit();
   }
   Window.LoadElement(e); doc.Editor.Regen(); Window.Notify("União concluída. "+duplicate+" ocorrências repetidas não foram contadas novamente. Origens preservadas.");
  }); }
  static ObjectId CalhaDefinition(Database db,Transaction tr) {return RectDefinition(db,tr,"DUALL-CALHA");}
  static ObjectId RectDefinition(Database db,Transaction tr,string name) {
   var bt=(BlockTable)tr.GetObject(db.BlockTableId,OpenMode.ForRead);
   if(bt.Has(name))return bt[name];
   bt.UpgradeOpen(); var def=new BlockTableRecord {Name=name,Origin=Point3d.Origin};
   var id=bt.Add(def); tr.AddNewlyCreatedDBObject(def,true);
   var poly=new Polyline(); poly.AddVertexAt(0,new Point2d(0,0),0,0,0); poly.AddVertexAt(1,new Point2d(1,0),0,0,0); poly.AddVertexAt(2,new Point2d(1,1),0,0,0); poly.AddVertexAt(3,new Point2d(0,1),0,0,0); poly.Closed=true;
   def.AppendEntity(poly); tr.AddNewlyCreatedDBObject(poly,true);
   var line=new Line(new Point3d(0,.5,0),new Point3d(1,.5,0)); def.AppendEntity(line); tr.AddNewlyCreatedDBObject(line,true); return id;
  }
  [CommandMethod("PLUINSERIRCALHA")]
  public void InsertGutter() { Run(doc=>Window.Notify("Calhas em standby nesta versão.")); }
  [CommandMethod("PLUINSERIRPRUMADA")]
  public void InsertRiser() { Insert("Prumada"); }
  [CommandMethod("PLUINSERIRCOLETOR")]
  public void InsertCollector() {Run(doc=>Window.Notify("Coletores em standby nesta versão."));}
  void Insert(string kind) { Run(doc=> { Meters(doc); var e=Window.GetElement(); e=Model.Copy(e); e.Id=Guid.NewGuid().ToString("N"); e.Kind=kind; e.Contributions.Clear(); e.Diameter=0; e.SuggestedDiameter=0; e.ManualDiameter=0; Model.Validate(e);
   var pt=doc.Editor.GetPoint("\nPonto de inserção: "); if(pt.Status!=PromptStatus.OK)return;
   ObjectId definition=ObjectId.Null;
   if(kind=="Prumada") {
    using(var tr=doc.Database.TransactionManager.StartTransaction()) { var bt=(BlockTable)tr.GetObject(doc.Database.BlockTableId,OpenMode.ForRead); if(bt.Has(RiserBlockName)) definition=bt[RiserBlockName]; }
    if(definition.IsNull) using(var source=new Database(false,true)) { source.ReadDwgFile(Path.Combine(Folder,RiserTemplate),FileOpenMode.OpenForReadAndAllShare,true,""); definition=doc.Database.Insert(RiserBlockName,source,true); }
   }
   using(var tr=doc.Database.TransactionManager.StartTransaction()) {
    if(kind!="Prumada")definition=RectDefinition(doc.Database,tr,kind=="Calha"?"DUALL-CALHA":"DUALL-COLETOR");
    var space=(BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId,OpenMode.ForWrite);
    var b=new BlockReference(pt.Value.TransformBy(doc.Editor.CurrentUserCoordinateSystem),definition); space.AppendEntity(b); tr.AddNewlyCreatedDBObject(b,true);if(kind=="Coletor")b.ScaleFactors=new Scale3d(e.Length,.1,1);
    Update(b,tr,e); Save(b,tr,e); Selected=b.ObjectId; SelectedDb=doc.Database; tr.Commit();
   }
   Window.LoadElement(e); doc.Editor.Regen();
  }); }
  [CommandMethod("PLUNOVAORIGEM")]
  public void NewOrigins() { Run(doc=> {
   var opts=new PromptSelectionOptions(); opts.MessageForAdding="\nSelecione áreas copiadas que representam NOVAS contribuições físicas: ";
   var r=doc.Editor.GetSelection(opts,new SelectionFilter(new[]{new TypedValue((int)DxfCode.Start,"LWPOLYLINE")}));if(r.Status!=PromptStatus.OK)return;
   using(var tr=doc.Database.TransactionManager.StartTransaction()) { foreach(var id in r.Value.GetObjectIds()) WriteText(tr.GetObject(id,OpenMode.ForWrite),tr,"DUALL_PLUVIAL_ORIGIN",Guid.NewGuid().ToString("N")); tr.Commit(); }
   Window.Notify("Novas identidades atribuídas às áreas. Registros anteriores permanecem nos blocos que já os receberam.");
  }); }
  [CommandMethod("PLUCOPIAINDEPENDENTE")]
  public void Independent() { Run(doc=> {CheckSelected(doc);var e=Window.GetElement();
   var response=System.Windows.Forms.MessageBox.Show("Use somente para uma nova prumada física, com áreas independentes. Continuar?","Cópia independente",System.Windows.Forms.MessageBoxButtons.YesNo);
   if(response!=System.Windows.Forms.DialogResult.Yes)return;
   var map=new Dictionary<string,string>();foreach(var c in e.Contributions){if(!map.ContainsKey(c.Origin))map[c.Origin]=Guid.NewGuid().ToString("N");c.Origin=map[c.Origin];}
   e.Id=Guid.NewGuid().ToString("N");using(var tr=doc.Database.TransactionManager.StartTransaction()){var b=(BlockReference)tr.GetObject(Selected,OpenMode.ForWrite);Update(b,tr,e);Save(b,tr,e);tr.Commit();}Window.LoadElement(e);
  }); }
  [CommandMethod("PLUTABELA")]
  public void Table() { Run(doc=> { CheckSelected(doc); Element e;
   using(var tr=doc.Database.TransactionManager.StartTransaction()) { e=Read((BlockReference)tr.GetObject(Selected,OpenMode.ForRead),tr); }
   if(e==null)throw new System.Exception("Salve o elemento antes de gerar a tabela.");
   var pt=doc.Editor.GetPoint("\nPonto da tabela: ");if(pt.Status!=PromptStatus.OK)return;
   using(var tr=doc.Database.TransactionManager.StartTransaction()) {
    var table=new Table(); table.TableStyle=doc.Database.Tablestyle; table.SetSize(e.Contributions.Count+5,5);table.SetRowHeight(.4);table.SetColumnWidth(2.5);table.Position=pt.Value.TransformBy(doc.Editor.CurrentUserCoordinateSystem);
    table.Cells.TextHeight=.15;table.Cells[0,0].TextString=e.Name+" - "+e.Kind+" - "+e.Service+" - "+e.City;
    string[] headers={"Origem","Pavimento","Área m²","Acréscimo m²","Q L/min"}; for(int c=0;c<5;c++)table.Cells[1,c].TextString=headers[c];
    int row=2;foreach(var x in e.Contributions) { string[] values={x.Name,x.Floor,x.Area.ToString("F3"),x.Extra.ToString("F3"),(x.Effective*e.Rain/60).ToString("F3")};for(int c=0;c<5;c++)table.Cells[row,c].TextString=values[c];row++; }
    table.Cells[row,0].TextString="TOTAL";table.Cells[row,2].TextString=e.Area.ToString("F3");table.Cells[row,4].TextString=e.Flow.ToString("F3");
    table.Cells[row+1,0].TextString="I="+e.Rain+" mm/h; TR="+e.ReturnYears+"; t="+e.Duration+" min";table.Cells[row+1,1].TextString=e.RainSource;
    table.Cells[row+2,0].TextString=e.Status; table.GenerateLayout();
    var space=(BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId,OpenMode.ForWrite);space.AppendEntity(table);tr.AddNewlyCreatedDBObject(table,true);tr.Commit();
   }
  }); }
  [CommandMethod("PLUVERIFICAR")]
  public void VerifyNative() {
   var report=new List<string>();
   try {
    using(var db=new Database(true,true)) {
     db.Insunits=UnitsValue.Meters;ObjectId definition;
     using(var source=new Database(false,true)){source.ReadDwgFile(Path.Combine(Folder,RiserTemplate),FileOpenMode.OpenForReadAndAllShare,true,"");definition=db.Insert(RiserBlockName,source,true);}
     ObjectId riserId;
     using(var tr=db.TransactionManager.StartTransaction()) {
      var model=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db),OpenMode.ForWrite);
      var b=new BlockReference(Point3d.Origin,definition);model.AppendEntity(b);tr.AddNewlyCreatedDBObject(b,true);riserId=b.ObjectId;
      if(!b.IsDynamicBlock || BlockName(b,tr)!=RiserBlockName)throw new System.Exception("Importação não preservou nome e bloco dinâmico.");report.Add("PASS: D-Prumada-Plu importado como bloco dinâmico.");
      foreach(DynamicBlockReferenceProperty p in b.DynamicBlockReferencePropertyCollection)report.Add("Propriedade: "+p.PropertyName+"; opções: "+String.Join(",",p.GetAllowedValues().Select(x=>Convert.ToString(x))));
      var e=new Element {Project="TESTE",City="São Paulo/SP",RainSource="Hipótese de teste",Rain=180};
      e.Contributions.Add(new Contribution {Origin="teste-area",Name="Sacada",Kind="Sacada",Floor="P01",Area=80,Extra=0,Drawing="Teste"});
      Update(b,tr,e);Save(b,tr,e);if(e.Diameter!=150)throw new System.Exception("Diâmetro esperado: 150 mm para 240 L/min.");report.Add("PASS: propriedade PRUMADA alterada para 150 mm.");
      if(b.Layer!="D-HID-PLU-TET")throw new System.Exception("Layer pluvial incorreto.");
      e.Service="Reuso";Update(b,tr,e);Save(b,tr,e);
      if(b.Layer!="D-HID-APV-TET" || e.Diameter!=150)throw new System.Exception("Layer reuso ou diâmetro incorreto.");
      report.Add("PASS: Pluvial/Reuso alteram somente sistema e layer, preservando diâmetro.");
      for(int n=0;n<20;n++)Model.AddContribution(e,new Contribution {Origin="area-"+n,Name="Teste "+n,Kind="Sacada",Floor="P01",Area=1});
      Update(b,tr,e);Save(b,tr,e);
      var stored=Read(b,tr);
      if(stored.Contributions.Count!=21 || stored.Area!=100)throw new System.Exception("Perda de registros ao salvar várias contribuições.");
      report.Add("PASS: 21 contribuições persistidas em Xrecord, total de 100 m².");
      tr.Commit();
     }
     using(var target=new Database(true,true)) {
      target.Insunits=UnitsValue.Meters;var mapping=new IdMapping();db.WblockCloneObjects(new ObjectIdCollection(new[]{riserId}),SymbolUtilityServices.GetBlockModelSpaceId(target),mapping,DuplicateRecordCloning.Ignore,false);
      using(var tr=target.TransactionManager.StartTransaction()) {
       var b=(BlockReference)tr.GetObject(mapping[riserId].Value,OpenMode.ForRead);var e=Read(b,tr);
       if(e==null || e.Area!=100 || e.Contributions.Count!=21 || e.Contributions[0].Origin!="teste-area" || !b.IsDynamicBlock)throw new System.Exception("Dados não sobreviveram à cópia entre bancos DWG.");
       report.Add("PASS: cópia entre DWGs preserva origem, área e bloco dinâmico.");
      }
      target.SaveAs(Path.Combine(Folder,"Teste-transferencia-1.2.dwg"),DwgVersion.Current);
     }
     db.SaveAs(Path.Combine(Folder,"Teste-Prumadas-1.2.dwg"),DwgVersion.Current);
    }
    using(var reopened=new Database(false,true)) {
     reopened.ReadDwgFile(Path.Combine(Folder,"Teste-Prumadas-1.2.dwg"),FileOpenMode.OpenForReadAndAllShare,true,"");
     using(var tr=reopened.TransactionManager.StartTransaction()) {
      var model=(BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(reopened),OpenMode.ForRead);int found=0;
      foreach(ObjectId id in model){var b=tr.GetObject(id,OpenMode.ForRead) as BlockReference;if(b!=null && Read(b,tr)!=null)found++;}
      if(found!=1)throw new System.Exception("Esperado 1 bloco com dados após reabertura.");report.Add("PASS: arquivo salvo e reaberto com a prumada e seus registros.");
     }
    }
    report.Add("Concluído. Biblioteca contém exemplos com dados fictícios; inserir novos elementos pelo painel.");
   }catch(System.Exception e){report.Add("FAIL: "+e.ToString());}
   File.WriteAllLines(Path.Combine(Folder,"verificacao-gstarcad-1.2.txt"),report,Encoding.UTF8);
   App.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n"+String.Join("\n",report)+"\n");
  }
 }
 public class GutterGrip : GripData {public int Axis;}
 public class GutterGrips : GripOverrule {
  public override void GetGripPoints(Entity entity,GripDataCollection grips,double viewSize,int gripSize,Vector3d viewDirection,GetGripPointsFlags flags) {
   base.GetGripPoints(entity,grips,viewSize,gripSize,viewDirection,flags);
   var b=entity as BlockReference;if(b==null || b.Database==null)return;
   try {using(var tr=b.Database.TransactionManager.StartOpenCloseTransaction()) {
    var e=Plugin.Read(b,tr);if(e==null || e.Kind!="Calha")return;
    grips.Add(new GutterGrip {Axis=0,GripPoint=new Point3d(1,0,0).TransformBy(b.BlockTransform)});
    grips.Add(new GutterGrip {Axis=1,GripPoint=new Point3d(0,1,0).TransformBy(b.BlockTransform)});
   }}catch(System.Exception) { /* Ordinary blocks must retain their native grips. */ }
  }
  public override void MoveGripPointsAt(Entity entity,GripDataCollection grips,Vector3d offset,MoveGripPointsFlags flags) {
   var custom=grips.Cast<GripData>().OfType<GutterGrip>().ToArray();
   if(custom.Length==0){base.MoveGripPointsAt(entity,grips,offset,flags);return;}
   var b=(BlockReference)entity;
   using(var tr=b.Database.TransactionManager.StartOpenCloseTransaction()) {
    var e=Plugin.Read(b,tr);if(e==null)return;
    var local=offset.TransformBy(Matrix3d.Rotation(-b.Rotation,Vector3d.ZAxis,Point3d.Origin));
    foreach(var grip in custom){if(grip.Axis==0)e.Length=Math.Max(.01,b.ScaleFactors.X+local.X);else e.Width=Math.Max(.01,b.ScaleFactors.Y+local.Y);}
    Plugin.Update(b,tr,e);Plugin.Save(b,tr,e);tr.Commit();
   }
  }
 }
}





