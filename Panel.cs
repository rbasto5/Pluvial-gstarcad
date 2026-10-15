using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Duall.Pluvial {
 public class Panel : Form {
  Element current=new Element();
  readonly Dictionary<string,TextBox> fields=new Dictionary<string,TextBox>();
  readonly ComboBox kind=new ComboBox();
  readonly ComboBox areaKind=new ComboBox();
  readonly ComboBox service=new ComboBox();
  readonly DataGridView grid=new DataGridView();
  readonly Label summary=new Label(),status=new Label();
  readonly CheckBox meters=new CheckBox();
  readonly TextBox areaName=new TextBox(),floorText=new TextBox(),extra=new TextBox();
  readonly NumericUpDown firstFloor=new NumericUpDown(),floorCount=new NumericUpDown();
  public bool MetersConfirmed {get{return meters.Checked;}}
  public Panel() {
   Text="Duall Pluvial | GstarCAD 2025"; Width=590;Height=720;MinimumSize=new Size(540,550);
   Font=new Font("Segoe UI",9);StartPosition=FormStartPosition.CenterScreen;
   var root=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(12)};
   root.RowStyles.Add(new RowStyle(SizeType.Absolute,38));root.RowStyles.Add(new RowStyle(SizeType.Absolute,55));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,115));Controls.Add(root);
   var title=new Label {Text="PLUVIAL  |  Áreas, calhas e prumadas",Dock=DockStyle.Fill,Font=new Font("Segoe UI",14,FontStyle.Bold)};root.Controls.Add(title,0,0);
   var toolbar=new FlowLayoutPanel {Dock=DockStyle.Fill};root.Controls.Add(toolbar,0,1);
   Button(toolbar,"Selecionar","PLUSELECIONAR");Button(toolbar,"+ Prumada","PLUINSERIRPRUMADA");Button(toolbar,"+ Calha","PLUINSERIRCALHA");Button(toolbar,"+ Coletor","PLUINSERIRCOLETOR");
   var tabs=new TabControl {Dock=DockStyle.Fill};root.Controls.Add(tabs,0,2);
   var project=Page(tabs,"Projeto / elemento");
   Field(project,"Projeto / obra","Project");Field(project,"Identificação do elemento","Name");
   kind.DropDownStyle=ComboBoxStyle.DropDownList;kind.Items.AddRange(new object[]{"Prumada","Calha","Coletor"});Row(project,"Tipo",kind);
   service.DropDownStyle=ComboBoxStyle.DropDownList;service.Items.AddRange(new object[]{"Pluvial","Reuso"});Row(project,"Sistema da prumada",service);
   Note(project,"Pluvial: D-HID-PLU-TET. Reuso: D-HID-APV-TET. O cálculo é igual nos dois sistemas.");
   Field(project,"Município / UF","City");Field(project,"Intensidade (mm/h)","Rain");Field(project,"TR (anos)","ReturnYears");Field(project,"Duração (min)","Duration");Field(project,"Fonte da chuva","RainSource");
   meters.Text="Confirmo que o desenho está em metros";meters.AutoSize=true;Row(project,"Unidades",meters);
   Note(project,"Os dados ficam no próprio bloco. Cópias para outro DWG preservam as contribuições; não há atualização automática do arquivo de origem.");
   var areas=Page(tabs,"Contribuições");
   areaName.Text="Área";Row(areas,"Nome da área",areaName);
   areaKind.DropDownStyle=ComboBoxStyle.DropDownList;areaKind.Items.AddRange(new object[]{"Telhado","Sacada","Implantação"});areaKind.SelectedIndex=0;Row(areas,"Origem",areaKind);
   floorText.Text="COBERTURA";Row(areas,"Pavimentos (separados por vírgula)",floorText);
   firstFloor.Minimum=0;firstFloor.Maximum=999;firstFloor.Value=1;floorCount.Minimum=1;floorCount.Maximum=200;floorCount.Value=1;
   Row(areas,"Tipo: primeiro pavimento",firstFloor);Row(areas,"Tipo: quantidade de repetições",floorCount);
   var gen=new System.Windows.Forms.Button {Text="Gerar lista P01, P02...",AutoSize=true};gen.Click+=(s,a)=>floorText.Text=String.Join(", ",Enumerable.Range((int)firstFloor.Value,(int)floorCount.Value).Select(n=>"P"+n.ToString("D2")));Row(areas,"Pavimento tipo",gen);
   extra.Text="0";Row(areas,"Acréscimo por área / pavimento (m²)",extra);
   var areaButtons=new FlowLayoutPanel {AutoSize=true};Button(areaButtons,"Selecionar áreas + atribuir","PLUADICIONAR");Button(areaButtons,"Unir tubos no destino","PLUUNIR");Row(areas,"Ações",areaButtons);
   var identityButtons=new FlowLayoutPanel {AutoSize=true};Button(identityButtons,"Áreas copiadas: nova origem","PLUNOVAORIGEM");Button(identityButtons,"Prumada: cópia independente","PLUCOPIAINDEPENDENTE");Row(areas,"Cópias independentes",identityButtons);
   Note(areas,"O acréscimo vale para CADA contorno e CADA pavimento selecionado. Use seleções separadas para acréscimos diferentes. Contornos devem ser simples e sem sobreposições.");
   var dim=Page(tabs,"Dimensões");
   Field(dim,"Comprimento da calha / tubo (m)","Length");Field(dim,"Largura interna da calha (m)","Width");Field(dim,"Altura total da calha (m)","Height");Field(dim,"Lâmina de água (m)","Water");Field(dim,"Declividade (%) — 0,5 para 0,5%","Slope");Field(dim,"Manning n","Roughness");
   Note(dim,"Calha retangular: Manning, expoente 0,667 como na planilha. Verificação de seção uniforme; curvas, bocais, saídas e bordo livre exigem conferência.");
   Note(dim,"Prumada: diâmetro pela tabela da sua planilha, limitado às medidas do bloco. Isso NÃO substitui os ábacos da NBR 10844. Comprimento informado fica registrado; não participa da tabela de referência.");
   Note(dim,"Coletor horizontal: Manning com lâmina de 2/3 do diâmetro interno, n e declividade informados. O diâmetro comercial deve ter diâmetro interno igual ou superior ao calculado.");
   var results=new TabPage("Registros");tabs.TabPages.Add(results);
   var resultsLayout=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};resultsLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));resultsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));resultsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,60));results.Controls.Add(resultsLayout);
   grid.Dock=DockStyle.Fill;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells;
   foreach(string name in new[]{"Área","Tipo","Pavimento","Base m²","Acréscimo m²","Origem DWG"})grid.Columns.Add(name,name);
   resultsLayout.Controls.Add(grid,0,0);
   var remove=new System.Windows.Forms.Button {Text="Remover registros selecionados (salvar para aplicar)",Dock=DockStyle.Fill};remove.Click+=(s,a)=> { foreach(DataGridViewRow row in grid.SelectedRows) current.Contributions.Remove((Contribution)row.Tag);RefreshRows();};resultsLayout.Controls.Add(remove,0,1);
   var export=new FlowLayoutPanel {Dock=DockStyle.Fill};Button(export,"Inserir tabela no DWG","PLUTABELA");var csv=new System.Windows.Forms.Button {Text="Exportar memória CSV",AutoSize=true};csv.Click+=(s,a)=>Export();export.Controls.Add(csv);resultsLayout.Controls.Add(export,0,2);
   var footer=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};footer.RowStyles.Add(new RowStyle(SizeType.Absolute,27));footer.RowStyles.Add(new RowStyle(SizeType.Percent,100));footer.RowStyles.Add(new RowStyle(SizeType.Absolute,32));root.Controls.Add(footer,0,3);
   summary.Dock=DockStyle.Fill;summary.Font=new Font(Font,FontStyle.Bold);footer.Controls.Add(summary,0,0);status.Dock=DockStyle.Fill;status.ForeColor=Color.DarkRed;footer.Controls.Add(status,0,1);
   var save=new System.Windows.Forms.Button {Text="Salvar no bloco e recalcular",Dock=DockStyle.Fill,BackColor=Color.FromArgb(28,90,125),ForeColor=Color.White};save.Click+=(s,a)=>Plugin.Request("PLUSALVAR");footer.Controls.Add(save,0,2);
   LoadElement(current);
   Shown+=(s,a)=>BeginInvoke(new Action(()=>{var bounds=Screen.FromControl(this).WorkingArea;Height=Math.Min(760,bounds.Height-60);Width=Math.Min(590,bounds.Width-40);Location=new Point(bounds.Right-Width-20,bounds.Top+20);}));
  }
  TableLayoutPanel Page(TabControl tabs,string title) {var page=new TabPage(title);tabs.TabPages.Add(page);var panel=new TableLayoutPanel {Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(8)};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,48));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,52));page.AutoScroll=true;page.Controls.Add(panel);return panel;}
  void Row(TableLayoutPanel p,string label,Control c) {int r=p.RowCount++;p.RowStyles.Add(new RowStyle(SizeType.AutoSize));p.Controls.Add(new Label {Text=label,AutoSize=true,Margin=new Padding(3,7,3,7)},0,r);c.Dock=DockStyle.Top;c.Margin=new Padding(3,5,3,5);p.Controls.Add(c,1,r);}
  void Field(TableLayoutPanel p,string label,string key) {var t=new TextBox();fields.Add(key,t);Row(p,label,t);}
  void Note(TableLayoutPanel p,string text) {var l=new Label {Text=text,AutoSize=true,MaximumSize=new Size(500,0),Margin=new Padding(3,12,3,8)};int r=p.RowCount++;p.Controls.Add(l,0,r);p.SetColumnSpan(l,2);}
  void Button(FlowLayoutPanel p,string text,string cmd) {var b=new System.Windows.Forms.Button {Text=text,AutoSize=true,Height=30};b.Click+=(s,a)=>Plugin.Request(cmd);p.Controls.Add(b);}
  double Number(string value,string label) {double d;if(!Double.TryParse(value.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out d) || Double.IsNaN(d) || Double.IsInfinity(d))throw new System.Exception("Número inválido: "+label);return d;}
  public Element GetElement() {
   var e=Model.Copy(current);
   foreach(var f in fields) {var member=typeof(Element).GetField(f.Key); if(member.FieldType==typeof(string)) member.SetValue(e,f.Value.Text.Trim());else if(member.FieldType==typeof(int)) {double n=Number(f.Value.Text,f.Key);if(n!=Math.Truncate(n) || n>10000 || n<1)throw new System.Exception("TR deve ser inteiro positivo.");member.SetValue(e,(int)n);}else member.SetValue(e,Number(f.Value.Text,f.Key));}
   e.Kind=Convert.ToString(kind.SelectedItem);e.Service=Convert.ToString(service.SelectedItem);return e;
  }
  public Contribution GetContribution() {var c=new Contribution {Name=areaName.Text.Trim(),Kind=Convert.ToString(areaKind.SelectedItem),Floor=floorText.Text,Extra=Number(extra.Text,"acréscimo")}; if(c.Name.Length==0 || c.Extra<0)throw new System.Exception("Informe nome e acréscimo não negativo.");return c;}
  public void LoadElement(Element e) {current=Model.Copy(e);foreach(var f in fields)f.Value.Text=Convert.ToString(typeof(Element).GetField(f.Key).GetValue(current),CultureInfo.CurrentCulture);kind.SelectedItem=e.Kind;service.SelectedItem=current.Service;RefreshRows();}
  void RefreshRows() {grid.Rows.Clear();foreach(var c in current.Contributions) {int n=grid.Rows.Add(c.Name,c.Kind,c.Floor,c.Area.ToString("F3"),c.Extra.ToString("F3"),c.Drawing);grid.Rows[n].Tag=c;}summary.Text=current.Contributions.Count+" ocorrências | "+current.Area.ToString("F2")+" m² | "+current.Flow.ToString("F2")+" L/min";status.Text=current.Status;}
  public void Notify(string text) {System.Windows.Forms.MessageBox.Show(this,text,"Pluvial");}
  void Export() {try {var e=GetElement();Model.Validate(e);using(var dialog=new SaveFileDialog {Filter="Memória CSV|*.csv",FileName=e.Name+"-pluvial.csv"}) {if(dialog.ShowDialog(this)!=DialogResult.OK)return;var lines=new List<string>();lines.Add("Projeto;Elemento;Município;I mm/h;TR anos;Duração min;Fonte");lines.Add(String.Join(";",new[]{e.Project,e.Name,e.City,e.Rain.ToString(),e.ReturnYears.ToString(),e.Duration.ToString(),e.RainSource}.Select(Quote)));lines.Add("Área;Tipo;Pavimento;Área base m2;Acréscimo m2;Vazão L/min;Origem;ID contribuição");foreach(var c in e.Contributions)lines.Add(String.Join(";",new[]{c.Name,c.Kind,c.Floor,c.Area.ToString(),c.Extra.ToString(),(c.Effective*e.Rain/60).ToString(),c.Drawing,c.Origin}.Select(Quote)));lines.Add("TOTAL;;;"+e.Area.ToString()+";;"+e.Flow.ToString());lines.Add("Status;"+Quote(e.Status));System.IO.File.WriteAllLines(dialog.FileName,lines,new System.Text.UTF8Encoding(true));}}catch(System.Exception e){Notify(e.Message);}}
  static string Quote(string s) {if(s==null)s="";if(s.StartsWith("=")||s.StartsWith("+")||s.StartsWith("-")||s.StartsWith("@"))s="'"+s;return "\""+s.Replace("\"","\"\"")+"\"";}
 }
}
