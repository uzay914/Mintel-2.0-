using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Runtime.InteropServices;

public class MintelOSControl : Form {
    readonly Color bg=Color.FromArgb(7,15,19), card=Color.FromArgb(17,30,35), mint=Color.FromArgb(93,239,176), gray=Color.FromArgb(151,177,174);
    Label title, subtitle, cores, threads, boost, cpuStat, memStat, runtimeStat, status, modeText, tempStat;
    ComboBox model, rgb;
    TrackBar power;
    TextBox log;
    Button turboButton, cacheButton, saveButton, startupButton;
    List<Control> dashboardControls=new List<Control>();
    Panel cpuPage, rgbPage, diagnosticsPage, activityPage, labPage, rgbPreview, tempGraph;
    TabControl v2Tabs;
    NotifyIcon trayIcon;
    PerformanceCounter cpuCounter;
    Label liveCpu, liveMemory, liveRam, updateStatus, gamingStatus, themeStatus, healthDetails;
    ComboBox gamingProfile;
    CheckBox gamingEnabled;
    ProgressBar gamingProgress;
    Color activeAccent;
    string activeThemeName="MINT GREEN";
    [StructLayout(LayoutKind.Sequential)]struct MemoryStatus{public uint length;public uint load;public ulong totalPhysical;public ulong availablePhysical;public ulong totalPage;public ulong availablePage;public ulong totalVirtual;public ulong availableVirtual;public ulong availableExtended;}
    [DllImport("kernel32.dll")]static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    Label labTemp, labFan, labStatus;
    TrackBar cooling;
    ProgressBar labProgress;
    List<int> temperatureHistory=new List<int>();
    Random rng=new Random();
    ListBox activityList;
    ListBox profileList;
    Label profileDetails, benchmarkLabel, diagnosticDetails;
    ProgressBar benchmarkProgress;
    bool turbo=false, cacheEnabled=true;
    DateTime opened=DateTime.Now;
    Timer timer=new Timer();
    string[] names={"Core M3-3200","Core M3-3400","Core M5-6500","Core M5-7200","Core M7-8700K","Core M7-9700X","Core M9-9950KS","Core M9-10950KS GS Edition","Core M9 GS Ultra Extreme 50900KS"};
    int[] coreCounts={4,6,8,10,12,16,24,40,128};
    int[] threadCounts={8,12,16,20,24,32,48,80,256};
    double[] speeds={3.8,4.2,5.0,5.5,6.2,6.8,7.2,9.0,12.5};
    string SettingsPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Mintel","control-settings.txt"); } }

    public MintelOSControl(){
        Text="MintelOS™ Control Center — Private Beta v2.0";
        ClientSize=new Size(960,710);MinimumSize=new Size(940,720);StartPosition=FormStartPosition.CenterScreen;
        BackColor=bg;ForeColor=Color.White;Font=new Font("Segoe UI",10);
        activeAccent=mint;
        var side=Panel(0,0,203,710,Color.FromArgb(10,24,27));side.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left;
        side.Controls.Add(Lbl("MINTEL",24,30,160,39,27,mint,true));
        side.Controls.Add(Lbl("OS CONTROL CENTER",24,72,170,22,9,gray,true));
        var dashNav=NavButton("● DASHBOARD",151);dashNav.Click+=(s,e)=>ShowPage("dashboard");side.Controls.Add(dashNav);
        var cpuNav=NavButton("▣ CPU PROFILES",204);cpuNav.Click+=(s,e)=>ShowPage("cpu");side.Controls.Add(cpuNav);
        var rgbNav=NavButton("✦ RGB SILICON",257);rgbNav.Click+=(s,e)=>ShowPage("rgb");side.Controls.Add(rgbNav);
        var diagNav=NavButton("◉ DIAGNOSTICS",310);diagNav.Click+=(s,e)=>ShowPage("diagnostics");side.Controls.Add(diagNav);
        var activityNav=NavButton("☷ ACTIVITY",363);activityNav.Click+=(s,e)=>ShowPage("activity");side.Controls.Add(activityNav);
        var labNav=NavButton("⚗ MINTLAB",416);labNav.Click+=(s,e)=>ShowPage("lab");side.Controls.Add(labNav);
        var v2Nav=NavButton("✦ V2 FEATURES",469);v2Nav.Click+=(s,e)=>ShowPage("v2");side.Controls.Add(v2Nav);
        side.Controls.Add(Lbl("PRIVATE BETA v2.0",24,625,170,25,10,Color.FromArgb(255,207,87),true));
        side.Controls.Add(Lbl("Fictional CPU controls\nReal VM readouts",24,653,170,34,8,gray,false));

        title=Lbl("MINTEL OS",235,24,550,45,27,Color.White,true);
        subtitle=Lbl("More cores. More cache. More mint.",236,72,550,25,11,gray,false);
        Controls.Add(title);Controls.Add(subtitle);
        runtimeStat=Lbl("Session: 00:00:00",760,68,165,24,10,gray,false);Controls.Add(runtimeStat);
        var banner=Panel(235,116,686,119,Color.FromArgb(18,54,44));
        dashboardControls.Add(banner);
        banner.Controls.Add(Lbl("VIRTUAL PROCESSOR PROFILE",20,14,360,22,10,mint,true));
        model=new ComboBox{Location=new Point(20,43),Size=new Size(460,32),DropDownStyle=ComboBoxStyle.DropDownList,BackColor=card,ForeColor=Color.White};
        model.Items.AddRange(names);model.SelectedIndex=7;model.SelectedIndexChanged+=(s,e)=>RefreshProfile();banner.Controls.Add(model);
        banner.Controls.Add(Lbl("SIMULATION ONLY",510,48,162,28,11,Color.FromArgb(255,205,85),true));

        cores=Metric(235,258,"MINT CORES", "40", 210);
        threads=Metric(473,258,"MINT THREADS", "80",210);
        boost=Metric(711,258,"BOOST GHz", "9.0",210);
        dashboardControls.Add(cores.Parent);dashboardControls.Add(threads.Parent);dashboardControls.Add(boost.Parent);

        var controls=Panel(235,375,686,145,card);
        dashboardControls.Add(controls);
        controls.Controls.Add(Lbl("MINTBOOST ENGINE",18,13,230,21,12,Color.White,true));
        turboButton=Btn("TURBO: OFF",18,48,144);turboButton.Click+=(s,e)=>{turbo=!turbo;turboButton.Text=turbo?"TURBO: ON":"TURBO: OFF";Log("MintBoost "+(turbo?"enabled":"disabled")+" (fictional). ");UpdateEstimate();};controls.Controls.Add(turboButton);
        controls.Controls.Add(Lbl("MINTCACHE",181,14,190,20,12,Color.White,true));
        cacheButton=Btn("CACHE: ON",181,48,144);cacheButton.Click+=(s,e)=>{cacheEnabled=!cacheEnabled;cacheButton.Text=cacheEnabled?"CACHE: ON":"CACHE: OFF";Log("MintCache "+(cacheEnabled?"enabled":"disabled")+" (fictional). ");};controls.Controls.Add(cacheButton);
        controls.Controls.Add(Lbl("RGB-COMPATIBLE SILICON™",345,14,295,20,12,Color.White,true));
        rgb=new ComboBox{Location=new Point(346,48),Size=new Size(300,34),DropDownStyle=ComboBoxStyle.DropDownList,BackColor=bg,ForeColor=Color.White};
        rgb.Items.AddRange(new object[]{"Mint Green","GS Red & Yellow","Arctic Blue","Rainbow Chaos","RGB Off"});rgb.SelectedIndex=0;
        rgb.SelectedIndexChanged+=(s,e)=>{Log("RGB profile: "+rgb.SelectedItem+" (visual preset).");UpdateRgbPreview();};controls.Controls.Add(rgb);
        controls.Controls.Add(Lbl("Fictional switches — no CPU overclocking or drivers changed.",18,110,650,21,9,gray,false));

        var footer=Panel(235,537,686,131,card);
        dashboardControls.Add(footer);
        footer.Controls.Add(Lbl("MINT POWER PROFILE",18,12,230,20,12,Color.White,true));
        power=new TrackBar{Location=new Point(13,38),Size=new Size(340,45),Minimum=1,Maximum=3,Value=2,TickStyle=TickStyle.Both,BackColor=card};
        power.ValueChanged+=(s,e)=>UpdateEstimate();footer.Controls.Add(power);
        modeText=Lbl("BALANCED MINT",368,48,240,24,12,mint,true);footer.Controls.Add(modeText);
        saveButton=Btn("SAVE SETTINGS",18,89,151);saveButton.Click+=(s,e)=>SaveSettings();footer.Controls.Add(saveButton);
        var folder=Btn("OPEN MINTEL FOLDER",187,89,200);folder.Click+=(s,e)=>OpenFolder();footer.Controls.Add(folder);
        var reset=Btn("RESET",404,89,120);reset.Click+=(s,e)=>ResetControls();footer.Controls.Add(reset);
        startupButton=Btn("STARTUP: OFF",540,89,138);startupButton.Click+=(s,e)=>ToggleStartup();footer.Controls.Add(startupButton);

        status=Lbl("SYSTEM READY  •  SIMULATION",235,683,500,20,10,mint,true);
        Controls.Add(status);
        log=new TextBox{Location=new Point(750,677),Size=new Size(170,25),Visible=false,Multiline=true};Controls.Add(log);
        BuildCpuPage();BuildRgbPage();BuildDiagnosticsPage();BuildActivityPage();BuildLabPage();BuildV2Tabs();InitializeTray();UpdateStartupButton();ShowPage("dashboard");
        RefreshProfile();LoadSettings();
        timer.Interval=1000;timer.Tick+=(s,e)=>{runtimeStat.Text="Session: "+(DateTime.Now-opened).ToString(@"hh\:mm\:ss");TickLab();UpdateLiveMonitor();};timer.Start();
        FormClosed+=(s,e)=>{timer.Stop();if(trayIcon!=null){trayIcon.Visible=false;trayIcon.Dispose();}if(cpuCounter!=null)cpuCounter.Dispose();};
    }
    Panel Panel(int x,int y,int w,int h,Color color){var p=new Panel{Location=new Point(x,y),Size=new Size(w,h),BackColor=color};Controls.Add(p);return p;}
    Label Lbl(string text,int x,int y,int w,int h,int size,Color color,bool bold){return new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),ForeColor=color,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular)};}
    Button Btn(string text,int x,int y,int w){return new Button{Text=text,Location=new Point(x,y),Size=new Size(w,33),FlatStyle=FlatStyle.Flat,BackColor=mint,ForeColor=Color.FromArgb(5,37,24),Font=new Font("Segoe UI",9,FontStyle.Bold)};}
    Button NavButton(string label,int y){var b=new Button{Text=label,Location=new Point(18,y),Size=new Size(174,39),FlatStyle=FlatStyle.Flat,TextAlign=ContentAlignment.MiddleLeft,BackColor=Color.FromArgb(10,24,27),ForeColor=mint,Font=new Font("Segoe UI",11,FontStyle.Bold)};b.FlatAppearance.BorderSize=0;return b;}
    void ShowPage(string page){foreach(Control c in dashboardControls)c.Visible=(page=="dashboard");if(cpuPage!=null)cpuPage.Visible=(page=="cpu");if(rgbPage!=null)rgbPage.Visible=(page=="rgb");if(diagnosticsPage!=null)diagnosticsPage.Visible=(page=="diagnostics");if(activityPage!=null)activityPage.Visible=(page=="activity");if(labPage!=null)labPage.Visible=(page=="lab");if(v2Tabs!=null)v2Tabs.Visible=(page=="v2");title.Text=page=="dashboard"?"MINTEL OS":page=="cpu"?"CPU PROFILES":page=="rgb"?"RGB SILICON":page=="diagnostics"?"DIAGNOSTICS":page=="lab"?"MINTLAB":page=="v2"?"V2 FEATURES":"ACTIVITY LOG";subtitle.Text=page=="dashboard"?"More cores. More cache. More mint.":page=="cpu"?"Choose your virtual Mintel processor.":page=="rgb"?"RGB-Compatible Silicon™ — fictional color studio.":page=="diagnostics"?"Real VM information and safe tests.":page=="lab"?"Thermal and cooling laboratory — fictional simulation.":page=="v2"?"Monitoring, gaming, updates, themes and system health.":"See what your Control Center has been doing.";if(page=="diagnostics")RefreshDiagnostics();}
    void BuildCpuPage(){
        cpuPage=Panel(235,116,686,552,card);cpuPage.Visible=false;
        cpuPage.Controls.Add(Lbl("MINTEL PROCESSOR CATALOG",22,20,420,26,15,mint,true));
        profileList=new ListBox{Location=new Point(22,58),Size=new Size(370,340),BackColor=bg,ForeColor=Color.White,Font=new Font("Segoe UI",11),BorderStyle=BorderStyle.None};
        profileList.Items.AddRange(names);profileList.SelectedIndex=7;profileList.SelectedIndexChanged+=(s,e)=>UpdateProfileDetails();cpuPage.Controls.Add(profileList);
        profileDetails=Lbl("",416,66,244,230,12,Color.White,false);cpuPage.Controls.Add(profileDetails);UpdateProfileDetails();
        var apply=Btn("USE SELECTED PROFILE",416,312,240);apply.Click+=(s,e)=>{if(profileList.SelectedIndex>=0){model.SelectedIndex=profileList.SelectedIndex;Log("Activated fictional "+model.SelectedItem);ShowPage("dashboard");}};cpuPage.Controls.Add(apply);
        cpuPage.Controls.Add(Lbl("MINTBENCH™ SIMULATOR",22,414,350,24,13,mint,true));
        benchmarkProgress=new ProgressBar{Location=new Point(22,449),Size=new Size(430,23)};cpuPage.Controls.Add(benchmarkProgress);
        benchmarkLabel=Lbl("Ready — this does not stress-test real hardware.",22,481,520,24,10,gray,false);cpuPage.Controls.Add(benchmarkLabel);
        var run=Btn("RUN MINTBENCH",481,447,175);run.Click+=async(s,e)=>await RunBenchmark(run);cpuPage.Controls.Add(run);
    }
    void UpdateProfileDetails(){if(profileDetails==null||profileList.SelectedIndex<0)return;int i=profileList.SelectedIndex;profileDetails.Text="MODEL\n"+names[i]+"\n\nCORES / THREADS\n"+coreCounts[i]+" / "+threadCounts[i]+"\n\nFICTIONAL BOOST\n"+speeds[i].ToString("0.0")+" GHz\n\nMintCache Certified™";}
    async Task RunBenchmark(Button button){button.Enabled=false;benchmarkProgress.Value=0;benchmarkLabel.Text="MintBench running (animation only)...";for(int n=0;n<=100;n+=4){benchmarkProgress.Value=n;await Task.Delay(55);}int i=profileList.SelectedIndex;int score=(i+1)*12750+coreCounts[i]*350;benchmarkLabel.Text="MintScore: "+score.ToString("N0")+" points (fictional result)";Log("MintBench finished — no hardware benchmark performed.");button.Enabled=true;}
    void BuildRgbPage(){
        rgbPage=Panel(235,116,686,552,card);rgbPage.Visible=false;
        rgbPage.Controls.Add(Lbl("RGB-COMPATIBLE SILICON™",23,20,500,32,18,mint,true));
        rgbPage.Controls.Add(Lbl("Select a simulated lighting color for your Mintel CPU.",23,61,600,28,11,gray,false));
        rgbPreview=new Panel{Location=new Point(23,110),Size=new Size(640,221),BackColor=mint};rgbPage.Controls.Add(rgbPreview);
        var chipText=Lbl("M",270,38,100,120,64,Color.FromArgb(7,20,18),true);chipText.TextAlign=ContentAlignment.MiddleCenter;chipText.BackColor=Color.Transparent;rgbPreview.Controls.Add(chipText);
        var presetNames=new[]{"MINT GREEN","GS EDITION","ARCTIC BLUE","RAINBOW CHAOS","RGB OFF"};
        for(int i=0;i<presetNames.Length;i++){int selected=i;var b=Btn(presetNames[i],23+(i%3)*213,353+(i/3)*54,195);b.Click+=(s,e)=>{rgb.SelectedIndex=selected;UpdateRgbPreview();};rgbPage.Controls.Add(b);}
        rgbPage.Controls.Add(Lbl("Visual preview only. No motherboard RGB controller is accessed.",23,482,620,35,11,gray,false));UpdateRgbPreview();
    }
    void UpdateRgbPreview(){if(rgbPreview==null||rgb==null)return;Color[] choices={mint,Color.FromArgb(219,50,48),Color.FromArgb(64,173,255),Color.FromArgb(191,87,234),Color.FromArgb(28,38,44)};rgbPreview.BackColor=choices[Math.Max(0,rgb.SelectedIndex)];}
    void BuildDiagnosticsPage(){
        diagnosticsPage=Panel(235,116,686,552,card);diagnosticsPage.Visible=false;
        diagnosticsPage.Controls.Add(Lbl("VM DIAGNOSTICS",23,20,500,30,18,mint,true));
        diagnosticDetails=Lbl("",23,74,628,290,12,Color.White,false);diagnosticsPage.Controls.Add(diagnosticDetails);
        var refresh=Btn("REFRESH DIAGNOSTICS",23,381,225);refresh.Click+=(s,e)=>RefreshDiagnostics();diagnosticsPage.Controls.Add(refresh);
        var export=Btn("EXPORT REPORT",266,381,175);export.Click+=(s,e)=>ExportDiagnostics();diagnosticsPage.Controls.Add(export);
        diagnosticsPage.Controls.Add(Lbl("This page only reads system information. It does not change drivers, firmware, or your boot logo.",23,458,634,62,11,gray,false));
    }
    void RefreshDiagnostics(){if(diagnosticDetails==null)return;long memory=Process.GetCurrentProcess().WorkingSet64/(1024*1024);diagnosticDetails.Text="WINDOWS VERSION  •  "+Environment.OSVersion.Version+"\n\nLOGICAL PROCESSORS  •  "+Environment.ProcessorCount+"\n\n64-BIT SYSTEM  •  "+Environment.Is64BitOperatingSystem+"\n\nCONTROL CENTER MEMORY  •  "+memory+" MB\n\nVM USER  •  "+Environment.UserName+"\n\nCURRENT SESSION  •  "+(DateTime.Now-opened).ToString(@"hh\:mm\:ss")+"\n\nMINTEL HARDWARE CONTROL  •  SIMULATED";Log("VM diagnostics refreshed.");}
    void ExportDiagnostics(){using(var dialog=new SaveFileDialog{FileName="mintel-diagnostics.txt",Filter="Text file (*.txt)|*.txt"}){if(dialog.ShowDialog()!=DialogResult.OK)return;try{File.WriteAllText(dialog.FileName,diagnosticDetails.Text+Environment.NewLine+"MintelOS Private Beta — Fictional CPU Brand");Log("Diagnostics exported.");}catch(Exception ex){MessageBox.Show(ex.Message,"Export failed");}}}
    void BuildActivityPage(){
        activityPage=Panel(235,116,686,552,card);activityPage.Visible=false;
        activityPage.Controls.Add(Lbl("MINTEL SESSION ACTIVITY",23,20,590,30,18,mint,true));
        activityPage.Controls.Add(Lbl("Local session events. No data is sent anywhere.",23,54,610,26,11,gray,false));
        activityList=new ListBox{Location=new Point(23,97),Size=new Size(640,337),BackColor=bg,ForeColor=mint,Font=new Font("Consolas",10),BorderStyle=BorderStyle.None};activityPage.Controls.Add(activityList);
        var export=Btn("EXPORT ACTIVITY",23,459,175);export.Click+=(s,e)=>ExportActivity();activityPage.Controls.Add(export);
        var clear=Btn("CLEAR DISPLAY",216,459,170);clear.Click+=(s,e)=>{activityList.Items.Clear();Log("Activity display cleared.");};activityPage.Controls.Add(clear);
        var about=Btn("ABOUT MINTEL OS",404,459,197);about.Click+=(s,e)=>MessageBox.Show("MintelOS Control Center — Private Beta v1.3\n\nFictional CPU profiles, MintBoost, MintCache, RGB and MintLab simulator.\n\nNo actual CPU settings, firmware, bootloader or drivers are changed.","About MintelOS");activityPage.Controls.Add(about);
    }
    void BuildLabPage(){
        labPage=Panel(235,116,686,552,card);labPage.Visible=false;
        labPage.Controls.Add(Lbl("MINTLAB™ THERMAL SIMULATOR",23,18,610,30,18,mint,true));
        labPage.Controls.Add(Lbl("FICTIONAL CPU TEMPERATURE",23,70,340,22,10,gray,true));
        labTemp=Lbl("42°C",23,98,290,59,35,mint,true);labPage.Controls.Add(labTemp);
        labPage.Controls.Add(Lbl("COOLING PROFILE",374,70,260,22,10,gray,true));
        cooling=new TrackBar{Location=new Point(363,105),Size=new Size(276,50),Minimum=1,Maximum=3,Value=2,TickStyle=TickStyle.Both,BackColor=card};
        cooling.ValueChanged+=(s,e)=>{UpdateCoolingLabel();Log("Cooling preset changed (simulation).");};labPage.Controls.Add(cooling);
        labFan=Lbl("BALANCED COOLING",376,162,268,30,12,mint,true);labPage.Controls.Add(labFan);
        labPage.Controls.Add(Lbl("SIMULATED THERMAL HISTORY",23,212,430,24,12,Color.White,true));
        tempGraph=new Panel{Location=new Point(23,249),Size=new Size(636,135),BackColor=bg};tempGraph.Paint+=(s,e)=>DrawThermalGraph(e.Graphics);labPage.Controls.Add(tempGraph);
        labPage.Controls.Add(Lbl("MINTSTABILITY™ TEST",23,404,350,22,12,mint,true));
        labProgress=new ProgressBar{Location=new Point(23,435),Size=new Size(408,23)};labPage.Controls.Add(labProgress);
        labStatus=Lbl("Ready — animation only, no real hardware stress.",23,466,410,22,10,gray,false);labPage.Controls.Add(labStatus);
        var test=Btn("RUN SIMULATION",454,433,203);test.Click+=async(s,e)=>await RunLabTest(test);labPage.Controls.Add(test);
        var export=Btn("EXPORT LAB REPORT",454,479,203);export.Click+=(s,e)=>ExportLabReport();labPage.Controls.Add(export);
        UpdateCoolingLabel();
    }
    void UpdateCoolingLabel(){if(cooling==null||labFan==null)return;labFan.Text=cooling.Value==1?"SILENT MINT":cooling.Value==2?"BALANCED COOLING":"NUCLEAR HOPE™";}
    void TickLab(){if(labTemp==null||cooling==null||model==null)return;int estimate=35+model.SelectedIndex*3+(turbo?6:0)+(power.Value==3?4:0)-cooling.Value*4+rng.Next(-2,3);labTemp.Text=estimate+"°C  SIMULATED";temperatureHistory.Add(estimate);if(temperatureHistory.Count>35)temperatureHistory.RemoveAt(0);if(tempGraph!=null)tempGraph.Invalidate();}
    void DrawThermalGraph(Graphics g){if(temperatureHistory.Count<2)return;using(var pen=new Pen(mint,3)){int n=temperatureHistory.Count;for(int i=1;i<n;i++){int x1=(i-1)*(tempGraph.Width-20)/34+10;int x2=i*(tempGraph.Width-20)/34+10;int y1=tempGraph.Height-10-(temperatureHistory[i-1]-15)*2;int y2=tempGraph.Height-10-(temperatureHistory[i]-15)*2;g.DrawLine(pen,x1,Math.Max(5,y1),x2,Math.Max(5,y2));}}}
    async Task RunLabTest(Button button){button.Enabled=false;labProgress.Value=0;labStatus.Text="Running fictional stability simulation...";for(int i=0;i<=100;i+=5){labProgress.Value=i;await Task.Delay(90);}labStatus.Text="STABLE — Mintel Certified™ (simulated)";Log("MintStability completed: stable fictional profile.");button.Enabled=true;}
    void ExportLabReport(){using(var dlg=new SaveFileDialog{FileName="mintel-lab-report.txt",Filter="Text file (*.txt)|*.txt"}){if(dlg.ShowDialog()!=DialogResult.OK)return;try{File.WriteAllText(dlg.FileName,"MintelOS Private Beta — MintLab Simulation\r\nProfile: "+model.SelectedItem+"\r\nCooling: "+labFan.Text+"\r\nTemperature: "+labTemp.Text+"\r\nThis is not a real hardware measurement.\r\n");Log("MintLab report exported.");}catch(Exception ex){MessageBox.Show(ex.Message,"Export failed");}}}
    TabPage NewTab(string name){var page=new TabPage(name){BackColor=card,ForeColor=Color.White,Padding=new Padding(12)};v2Tabs.TabPages.Add(page);return page;}
    void BuildV2Tabs(){
        v2Tabs=new TabControl{Location=new Point(235,116),Size=new Size(686,552),Visible=false,Font=new Font("Segoe UI",9,FontStyle.Bold)};Controls.Add(v2Tabs);
        var monitor=NewTab("Monitor");monitor.Controls.Add(Lbl("LIVE PERFORMANCE MONITOR",20,25,560,32,18,mint,true));
        monitor.Controls.Add(Lbl("Real Windows CPU usage where available; process memory measured locally.",20,65,630,44,11,gray,false));
        liveCpu=Lbl("CPU: initializing...",20,145,590,55,24,mint,true);monitor.Controls.Add(liveCpu);
        liveRam=Lbl("SYSTEM RAM: initializing...",20,225,600,38,18,Color.White,true);monitor.Controls.Add(liveRam);
        liveMemory=Lbl("APP MEMORY: -- MB",20,294,600,40,16,gray,true);monitor.Controls.Add(liveMemory);
        monitor.Controls.Add(Lbl("Fictional MintBoost clocks are separate from these real readouts.",20,380,610,45,11,gray,false));
        try{cpuCounter=new PerformanceCounter("Processor","% Processor Time","_Total");cpuCounter.NextValue();}catch{cpuCounter=null;}

        var tray=NewTab("System Tray");tray.Controls.Add(Lbl("MINTEL SYSTEM TRAY",20,25,590,35,18,mint,true));
        tray.Controls.Add(Lbl("A MintelOS icon lives beside the Windows clock while this application is running.",20,80,610,60,12,gray,false));
        var showTray=Btn("SHOW TRAY BALLOON",20,175,205);showTray.Click+=(s,e)=>{if(trayIcon!=null)trayIcon.ShowBalloonTip(2500,"MintelOS Private Beta","More cores. More cache. More mint.",ToolTipIcon.Info);};tray.Controls.Add(showTray);
        var hide=Btn("MINIMIZE TO TRAY",245,175,205);hide.Click+=(s,e)=>{WindowState=FormWindowState.Minimized;Hide();};tray.Controls.Add(hide);
        tray.Controls.Add(Lbl("Double-click the tray icon to reopen the window. Use its menu to exit.",20,255,600,60,11,gray,false));

        var updates=NewTab("Updates");updates.Controls.Add(Lbl("MINTEL UPDATE CENTER",20,25,590,34,18,mint,true));
        updates.Controls.Add(Lbl("Fictional update catalog — this tab makes no network requests.",20,64,600,32,11,gray,false));
        var updateList=new ListBox{Location=new Point(20,112),Size=new Size(610,198),BackColor=bg,ForeColor=mint,Font=new Font("Consolas",11)};updateList.Items.AddRange(new object[]{"MintBoost Engine 2.1  -  Demo update","MintCache Optimizer 4.0  -  Demo update","RGB Silicon Suite 3.2  -  Demo update","MintelOS Private Beta 2.0  -  Current"});updates.Controls.Add(updateList);
        updateStatus=Lbl("Ready for simulated update scan.",20,326,600,32,11,gray,false);updates.Controls.Add(updateStatus);
        var check=Btn("CHECK UPDATES",20,373,181);check.Click+=async(s,e)=>await SimulateUpdateScan(check);updates.Controls.Add(check);
        var details=Btn("VIEW CHANGELOG",220,373,181);details.Click+=(s,e)=>MessageBox.Show("MintelOS v2.0\n• Live performance readouts\n• System tray integration\n• Gaming Mode\n• Theme Studio\n• Health checks\n\nOther update entries are fictional.","MintelOS Changelog");updates.Controls.Add(details);

        var game=NewTab("Gaming");game.Controls.Add(Lbl("MINTEL GAMING MODE",20,25,580,34,18,mint,true));
        game.Controls.Add(Lbl("Select a demo game preset. This does not alter Windows performance settings.",20,66,612,45,11,gray,false));
        gamingProfile=new ComboBox{Location=new Point(20,133),Size=new Size(390,33),DropDownStyle=ComboBoxStyle.DropDownList,BackColor=bg,ForeColor=Color.White};gamingProfile.Items.AddRange(new object[]{"Roblox MintBoost","Football GS Edition","Streaming + Homework","Ultra Mint Chaos"});gamingProfile.SelectedIndex=0;game.Controls.Add(gamingProfile);
        gamingEnabled=new CheckBox{Text="Enable simulated Gaming Mode",Location=new Point(20,196),Size=new Size(350,31),ForeColor=mint};gamingEnabled.CheckedChanged+=(s,e)=>Log("Gaming Mode "+(gamingEnabled.Checked?"enabled":"disabled")+" (simulated).");game.Controls.Add(gamingEnabled);
        gamingProgress=new ProgressBar{Location=new Point(20,261),Size=new Size(610,23)};game.Controls.Add(gamingProgress);
        gamingStatus=Lbl("Ready for a fictional FPS optimization simulation.",20,310,600,30,11,gray,false);game.Controls.Add(gamingStatus);
        var optimize=Btn("SIMULATE OPTIMIZATION",20,370,250);optimize.Click+=async(s,e)=>await SimulateGame(optimize);game.Controls.Add(optimize);

        var themes=NewTab("Themes");themes.Controls.Add(Lbl("MINTEL THEME STUDIO",20,25,560,35,18,mint,true));
        themes.Controls.Add(Lbl("Change Control Center colors only. Windows and the boot logo are untouched.",20,69,610,43,11,gray,false));
        string[] labels={"MINT GREEN","GS GOLD","ARCTIC BLUE","MINT PURPLE"};Color[] colors={Color.FromArgb(93,239,176),Color.FromArgb(255,204,55),Color.FromArgb(75,174,255),Color.FromArgb(196,107,248)};
        for(int i=0;i<labels.Length;i++){int k=i;var b=Btn(labels[i],20+(i%2)*295,146+(i/2)*74,265);b.BackColor=colors[i];b.Click+=(s,e)=>ApplyTheme(colors[k],labels[k]);themes.Controls.Add(b);}
        themeStatus=Lbl("Current theme: MINT GREEN",20,338,560,33,12,mint,true);themes.Controls.Add(themeStatus);
        var restore=Btn("RESTORE ORIGINAL MINT",20,397,280);restore.Click+=(s,e)=>ApplyTheme(colors[0],labels[0]);themes.Controls.Add(restore);

        var health=NewTab("Health");health.Controls.Add(Lbl("MINTEL SYSTEM HEALTH",20,25,560,35,18,mint,true));
        healthDetails=Lbl("",20,84,620,318,12,Color.White,false);health.Controls.Add(healthDetails);
        var healthButton=Btn("RUN HEALTH CHECK",20,414,224);healthButton.Click+=(s,e)=>RefreshHealth();health.Controls.Add(healthButton);
        RefreshHealth();
    }
    void UpdateLiveMonitor(){if(liveCpu!=null){try{liveCpu.Text=cpuCounter==null?"CPU USAGE: unavailable":"CPU USAGE: "+cpuCounter.NextValue().ToString("0")+"%";}catch{liveCpu.Text="CPU USAGE: unavailable";}}if(liveMemory!=null)liveMemory.Text="APP MEMORY: "+(Process.GetCurrentProcess().WorkingSet64/(1024*1024))+" MB";if(liveRam!=null){try{var m=new MemoryStatus();m.length=(uint)Marshal.SizeOf(typeof(MemoryStatus));if(GlobalMemoryStatusEx(ref m)){ulong used=m.totalPhysical-m.availablePhysical;liveRam.Text="SYSTEM RAM: "+(used/(1024.0*1024*1024)).ToString("0.0")+" / "+(m.totalPhysical/(1024.0*1024*1024)).ToString("0.0")+" GB ("+m.load+"%)";}else liveRam.Text="SYSTEM RAM: unavailable";}catch{liveRam.Text="SYSTEM RAM: unavailable";}}}
    void InitializeTray(){Icon appIcon=SystemIcons.Application;try{appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);}catch{}Icon=appIcon;trayIcon=new NotifyIcon{Icon=appIcon,Text="MintelOS Control Center",Visible=true};var menu=new ContextMenu();menu.MenuItems.Add("Open MintelOS",(s,e)=>RestoreWindow());menu.MenuItems.Add("Gaming Mode (Simulated)",(s,e)=>{ShowPage("v2");v2Tabs.SelectedIndex=3;RestoreWindow();gamingEnabled.Checked=!gamingEnabled.Checked;});menu.MenuItems.Add("Exit",(s,e)=>Close());trayIcon.ContextMenu=menu;trayIcon.DoubleClick+=(s,e)=>RestoreWindow();}
    void RestoreWindow(){Show();WindowState=FormWindowState.Normal;Activate();}
    async Task SimulateUpdateScan(Button button){button.Enabled=false;updateStatus.Text="Checking fictional Mintel update catalog...";await Task.Delay(900);updateStatus.Text="3 demo updates available. No files downloaded or installed.";Log("Update scan completed (offline simulation).");button.Enabled=true;}
    async Task SimulateGame(Button button){if(!gamingEnabled.Checked){gamingStatus.Text="Enable Gaming Mode first.";return;}button.Enabled=false;gamingProgress.Value=0;for(int i=0;i<=100;i+=5){gamingProgress.Value=i;await Task.Delay(65);}gamingStatus.Text=gamingProfile.SelectedItem+": +42 imaginary FPS!";Log("Gaming optimization simulation completed.");button.Enabled=true;}
    void Recolor(Control root,Color oldColor,Color next){if(root.ForeColor==oldColor)root.ForeColor=next;if(root is Button&&root.BackColor==oldColor)root.BackColor=next;foreach(Control c in root.Controls)Recolor(c,oldColor,next);}
    void ApplyTheme(Color color,string name){Color old=activeAccent;Recolor(this,old,color);activeAccent=color;activeThemeName=name;if(themeStatus!=null)themeStatus.Text="Current theme: "+name;Log("Theme changed to "+name+" (app only).");}
    void RefreshHealth(){if(healthDetails==null)return;string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Mintel");string[] files={"minteldriver.bat","mintel.sys","mintel.mint"};int present=0;foreach(string f in files)if(File.Exists(Path.Combine(folder,f)))present++;string disk="Unavailable";try{string root=Path.GetPathRoot(Environment.SystemDirectory);var drive=new DriveInfo(root);disk=(drive.AvailableFreeSpace/(1024L*1024L*1024L))+" GB free";}catch{}string startup="UNKNOWN";try{startup=StartupEnabled()?"ENABLED":"DISABLED";}catch{}healthDetails.Text="WINDOWS BUILD  •  "+Environment.OSVersion.Version+"\n\nSYSTEM DRIVE  •  "+disk+"\n\nLOGICAL PROCESSORS  •  "+Environment.ProcessorCount+"\n\nMINTEL DEMO FILES  •  "+present+" / 3 present\n\nAPP STARTUP  •  "+startup+"\n\nBOOT LOGO  •  NOT MODIFIED BY THIS APP\n\nSTATUS  •  READ-ONLY HEALTH CHECK";Log("System health check refreshed.");}
    void ExportActivity(){using(var dlg=new SaveFileDialog{FileName="mintel-activity.txt",Filter="Text file (*.txt)|*.txt"}){if(dlg.ShowDialog()!=DialogResult.OK)return;try{var lines=new List<string>();foreach(var item in activityList.Items)lines.Add(item.ToString());File.WriteAllLines(dlg.FileName,lines.ToArray());Log("Activity exported.");}catch(Exception ex){MessageBox.Show(ex.Message,"Export failed");}}}
    const string StartupKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
    const string StartupName="MintelOSControlPrivateBeta";
    string StartupValue(){using(var key=Registry.CurrentUser.OpenSubKey(StartupKey)){return key==null?"":Convert.ToString(key.GetValue(StartupName,""));}}
    bool StartupEnabled(){return string.Equals(StartupValue(),"\""+Application.ExecutablePath+"\"",StringComparison.OrdinalIgnoreCase);}
    void UpdateStartupButton(){if(startupButton==null)return;try{startupButton.Text=StartupEnabled()?"STARTUP: ON":"STARTUP: OFF";}catch{startupButton.Text="STARTUP: ?";}}
    void ToggleStartup(){try{bool enabled=StartupEnabled();if(!enabled){if(MessageBox.Show("Start MintelOS Control Center automatically when you sign in to Windows?\n\nKeep the EXE in its current folder. This won't affect the Windows boot logo.","Enable MintelOS Startup",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;using(var key=Registry.CurrentUser.CreateSubKey(StartupKey)){if(key==null)throw new InvalidOperationException("Could not open startup registry key.");key.SetValue(StartupName,"\""+Application.ExecutablePath+"\"");}Log("Windows sign-in launch enabled (current user only).");}else{using(var key=Registry.CurrentUser.OpenSubKey(StartupKey,true)){if(key!=null)key.DeleteValue(StartupName,false);}Log("Windows sign-in launch disabled.");}UpdateStartupButton();}catch(Exception ex){MessageBox.Show(ex.Message,"Startup setting failed");}}
    Label Metric(int x,int y,string name,string val,int w){var p=Panel(x,y,w,100,card);p.Controls.Add(Lbl(name,14,13,w-25,22,10,gray,true));var v=Lbl(val,14,40,w-25,48,25,mint,true);p.Controls.Add(v);return v;}
    void RefreshProfile(){if(model==null||model.SelectedIndex<0||cores==null)return;int i=model.SelectedIndex;cores.Text=coreCounts[i].ToString();threads.Text=threadCounts[i].ToString();UpdateEstimate();}
    void UpdateEstimate(){if(model==null||boost==null||power==null)return;int i=model.SelectedIndex;double value=speeds[i]+(turbo?0.5:0)-(power.Value==1?0.3:0);boost.Text=value.ToString("0.0");if(modeText!=null)modeText.Text=power.Value==1?"ECO MINT":power.Value==2?"BALANCED MINT":"MAXIMUM MINT";}
    void Log(string entry){if(status!=null)status.Text=entry.Trim();if(activityList!=null){activityList.Items.Add(DateTime.Now.ToString("HH:mm:ss")+"  "+entry.Trim());if(activityList.Items.Count>200)activityList.Items.RemoveAt(0);activityList.TopIndex=activityList.Items.Count-1;}}
    void OpenFolder(){string path=Path.GetDirectoryName(SettingsPath);Directory.CreateDirectory(path);Process.Start("explorer.exe",path);}
    void ResetControls(){model.SelectedIndex=7;turbo=false;cacheEnabled=true;turboButton.Text="TURBO: OFF";cacheButton.Text="CACHE: ON";rgb.SelectedIndex=0;power.Value=2;UpdateEstimate();Log("Defaults restored (simulation).");}
    void SaveSettings(){try{Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));File.WriteAllLines(SettingsPath,new[]{model.SelectedIndex.ToString(),turbo.ToString(),cacheEnabled.ToString(),rgb.SelectedIndex.ToString(),power.Value.ToString(),activeAccent.ToArgb().ToString(),activeThemeName});Log("MintelOS settings saved for this Windows user.");}catch(Exception ex){MessageBox.Show(ex.Message,"Save failed");}}
    void LoadSettings(){try{if(!File.Exists(SettingsPath))return;string[] lines=File.ReadAllLines(SettingsPath);if(lines.Length<5)return;int n;if(int.TryParse(lines[0],out n)&&n>=0&&n<names.Length)model.SelectedIndex=n;bool b;if(bool.TryParse(lines[1],out b))turbo=b;if(bool.TryParse(lines[2],out b))cacheEnabled=b;if(int.TryParse(lines[3],out n)&&n>=0&&n<rgb.Items.Count)rgb.SelectedIndex=n;if(int.TryParse(lines[4],out n)&&n>=1&&n<=3)power.Value=n;if(lines.Length>=7&&int.TryParse(lines[5],out n))ApplyTheme(Color.FromArgb(n),lines[6]);turboButton.Text=turbo?"TURBO: ON":"TURBO: OFF";cacheButton.Text=cacheEnabled?"CACHE: ON":"CACHE: OFF";UpdateEstimate();}catch(Exception ex){Log("Settings could not be loaded: "+ex.Message);}}
    [STAThread]public static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MintelOSControl());}
}
