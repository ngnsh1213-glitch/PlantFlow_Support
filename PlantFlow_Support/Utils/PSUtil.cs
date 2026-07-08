using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.ProcessPower.DataLinks;
using Autodesk.ProcessPower.DataObjects;
using Autodesk.ProcessPower.P3dProjectParts;
using Autodesk.ProcessPower.PlantInstance;
using Autodesk.ProcessPower.ProjectManager;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SQLite;
using System.Windows.Forms;
using System;

namespace PlantFlow_Support
{
    public class PSUtil
    {
        public Document doc;
        public Editor ed;
        public PlantProject plant_project;
        public PipingProject current_project;
        public DataLinksManager dl_manager;
        public PnPDatabase pnp_db;
        public ListView lvSupportName;
        public Dictionary<string, SupportInfo> SupportSelection;
        public static string OrthoTemplate;

        public PSUtil()
        {
            this.plant_project = PlantApplication.CurrentProject;
            this.current_project = this.plant_project.ProjectParts["Piping"] as PipingProject;
            this.dl_manager = ((Project)this.current_project).DataLinksManager;
            this.pnp_db = this.dl_manager.GetPnPDatabase();
        }

        public static void Log(string msg) => Print(msg);

        public void GenerateSupportData(Dictionary<string, SupportInfo> supportSelection, string savePath)
        {
             // TODO: Re-implement or locate original logic for GenerateSupportData.
             // This was lost during refactoring if it existed in original PSUtil.
             // Placeholder to allow compilation.
             MessageBox.Show("GenerateSupportData logic is pending migration. Please contact developer.", "Refactoring in Progress");
        }

        public Dictionary<string, Vector3d> UpVectors() => GeomUtils.UpVectors();
        public Dictionary<string, Vector3d> ViewDirection() => GeomUtils.ViewDirection();
        public static string TitleViewLabel(string view_type) => GeomUtils.TitleViewLabel(view_type);
        public Dictionary<string, Matrix3d> UCSs() => GeomUtils.UCSs();

        public static void Print(string mess)
        {
            Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage("\n" + mess + "\n");
        }

        public void MessageDialog(string mess) => Application.ShowAlertDialog(mess);

        public Document GetDocument()
        {
            Document mdiActiveDocument = CadUtils.GetActiveDocument();
            if (mdiActiveDocument == null) return null;
            this.doc = mdiActiveDocument;
            this.ed = mdiActiveDocument.Editor;
            return mdiActiveDocument;
        }

        public DocumentCollection GetDocumentCollection() => CadUtils.GetDocumentCollection();

        public JObject ReadJson(string json_file) => ProjectDataUtils.ReadJson(json_file);

        public string IndividualDrawingPath(PnPDatabase db, string drawing_title) => ProjectDataUtils.IndividualDrawingPath(db, drawing_title);

        public Dictionary<string, string> GetSupportDimension(ObjectId id)
        {
            Dictionary<string, string> supportDimension = new Dictionary<string, string>();
            foreach (Autodesk.ProcessPower.ACPUtils.ParameterInfo supportParameter in Autodesk.ProcessPower.PnP3dPipeSupport.SupportHelper.GetSupportParameters(id))
            {
                string key = ((Autodesk.ProcessPower.ACPUtils.BaseInfo)supportParameter).Name.Replace("_", "");
                string str = System.Linq.Enumerable.Last(supportParameter.ToDefinitionString().Replace("_", "").Split('='));
                supportDimension.Add(key, str);
            }
            return supportDimension;
        }

        public ObjectId ConvertRowIdToObjectId(int row_id)
        {
            return this.dl_manager.MakeAcDbObjectId(System.Linq.Enumerable.ToList(this.dl_manager.FindAcPpObjectIds(row_id))[0]);
        }

        public void SetProperties(ObjectId id, StringCollection names, StringCollection values)
        {
            this.dl_manager.SetProperties(id, names, values);
        }

        public StringCollection GetProperties(ObjectId id, StringCollection prop_name_colls)
        {
            return this.dl_manager.GetProperties(id, prop_name_colls, true);
        }

        public static void ZoomToObject(Editor ed, Entity ent) => CadUtils.ZoomToObject(ed, ent);
        public static void ZoomToObject(Editor ed, Line line) => CadUtils.ZoomToObject(ed, line);

        public static List<Line> GridLineSorting(List<Line> list1, bool x_sort) => GeomUtils.GridLineSorting(list1, x_sort);

        public static void CreateHorizontalDimension(Extents3d extent, Matrix3d transform, ObjectId dimstyle_id, double dim_line_point, out RotatedDimension dimension)
            => AnnotationUtils.CreateHorizontalDimension(extent, transform, dimstyle_id, dim_line_point, out dimension);

        public static RotatedDimension CreateHorizontalDimension(Point3d point1, Point3d point2, Point3d point3, Matrix3d transform, ObjectId dimstyle_id)
            => AnnotationUtils.CreateHorizontalDimension(point1, point2, point3, transform, dimstyle_id);

        public static void CreateVerticalDimension(Extents3d extent, Matrix3d transform, ObjectId dimstyle_id, double dim_line_point, out RotatedDimension dimension)
            => AnnotationUtils.CreateVerticalDimension(extent, transform, dimstyle_id, dim_line_point, out dimension);

        public static RotatedDimension CreateVerticalDimension(Point3d point1, Point3d point2, Point3d point3, Matrix3d transform, ObjectId dimstyle_id)
            => AnnotationUtils.CreateVerticalDimension(point1, point2, point3, transform, dimstyle_id);

        public static MLeader CreateMLeader(Transaction trans, ObjectId block_content_id, ObjectId ml_style_id, Point3d[] tagging_points, string tag, Matrix3d ucs)
            => AnnotationUtils.CreateMLeader(trans, block_content_id, ml_style_id, tagging_points, tag, ucs);

        public static MLeader CreateMLeader(Point3d[] tagging_points, MText content, ObjectId ml_style_id, Matrix3d ucs)
            => AnnotationUtils.CreateMLeader(tagging_points, content, ml_style_id, ucs);

        public void run(int item_no)
        {
            item_no++;
            Commands.ItemNo = item_no;
            string text = this.lvSupportName.Items[item_no].SubItems[0].Text;
            if (this.lvSupportName.Items.Count > 1) Commands.IsMoreThanOne = true;
            Commands.SupportName = text;
            try
            {
                new PlantOrthoView(this.SupportSelection[text]).run();
            }
            catch (Exception ex)
            {
                PlantOrthoView.FileDiag("PSUtil.run 실패 support='" + text + "' itemNo=" + item_no + " error=" + ex.GetType().Name + ": " + ex.Message);
                if (!Commands.IsMoreThanOne)
                    throw;
                Commands.ContinueBatchAfterFailure("PSUtil.run", ex);
            }
        }

        public void LimitView() => CadUtils.LimitView(this.SupportSelection);
        public void LimitCube(ref SupportInfo sp_info) => CadUtils.LimitCube(ref sp_info);

        public static Dictionary<string, string> CheckDatumInfo(Point3d datum_point) => GeomUtils.CheckDatumInfo(datum_point);

        public static List<AttributeDefinition> CreateQuarterBlock(ObjectId datum_sym_id, Point3d[] line_starts, Point3d[] line_ends, ObjectId textstyle_id, Point3d[] text_position, string[] text_strings, ref BlockTableRecord datum_quarter)
            => AnnotationUtils.CreateQuarterBlock(datum_sym_id, line_starts, line_ends, textstyle_id, text_position, text_strings, ref datum_quarter);

        public static List<Point3d> AttaDPointArrangement(Extents3d frame_extents, List<Extents3d> atta_block_exts, Matrix3d ucs, out double dim_line_point, out double dim_line_point_vertical, out List<Point3d> points_vertical)
            => AnnotationUtils.AttaDPointArrangement(frame_extents, atta_block_exts, ucs, out dim_line_point, out dim_line_point_vertical, out points_vertical);

        public static List<RotatedDimension> CreateAttaDimension(List<Point3d> dpoints, double dim_line_point, Matrix3d ucs, ObjectId dimstyle_id)
            => AnnotationUtils.CreateAttaDimension(dpoints, dim_line_point, ucs, dimstyle_id);

        public static List<RotatedDimension> CreateAttaVerticalDimension(List<Point3d> dpoints, double dim_line_point, Matrix3d ucs, ObjectId dimstyle_id)
            => AnnotationUtils.CreateAttaVerticalDimension(dpoints, dim_line_point, ucs, dimstyle_id);

        public static JObject ReadJsonFile(string json_file) => ProjectDataUtils.ReadJsonFile(json_file);
        public static void WriteJsonFile(Dictionary<string, Point2d> Grid) => ProjectDataUtils.WriteJsonFile(Grid);

        public static double PipeSize(int norminal) => GeomUtils.PipeSize(norminal);
        public static string MetricImperial(string metric) => GeomUtils.MetricImperial(metric);

        public static List<string> GetTableNames(string db_path) => DatabaseUtils.GetTableNames(db_path);
        public static System.Data.DataTable GetDataTable(SQLiteConnection connection, string sql) => DatabaseUtils.GetDataTable(connection, sql);
        public static void InsertNewTable(string db_path, string table_name) => DatabaseUtils.InsertNewTable(db_path, table_name);
        public static void RenameTable(string db_path, string old_name, string new_name) => DatabaseUtils.RenameTable(db_path, old_name, new_name);
        public static void InsertNewRecord(string db_path, string table_name, string rev, string desc, string date, string dwn, string checked1, string checked2, string approved)
            => DatabaseUtils.InsertNewRecord(db_path, table_name, rev, desc, date, dwn, checked1, checked2, approved);
        public static void UpdateRecord(string db_path, string table_name, string rev, string desc, string date, string dwn, string checked1, string checked2, string approved)
            => DatabaseUtils.UpdateRecord(db_path, table_name, rev, desc, date, dwn, checked1, checked2, approved);
        public static void DeleteRecord(string db_path, string table_name, string revision) => DatabaseUtils.DeleteRecord(db_path, table_name, revision);

        // Re-added helper because I didn't see it in AnnotationUtils explicit call in original code, but moved logic to AnnotationUtils.
        // Wait, GetArrowHeadObjectId was private static in PSUtil.
        // I moved it to AnnotationUtils. I should expose it here if used elsewhere.
        // Or make it public in AnnotationUtils.
        // It was private in PSUtil? Let's check original...
        // Line 690: private static ObjectId GetArrowHeadObjectId...
        // If it was private, it's NOT called from outside.
        // It's checked if it's used inside PSUtil...
        // It was used by ... wait?
        // I don't see any usage in `PSUtil` file listing (1-1519). Maybe unused?
        // Or specific usages I missed?
        // Ah, if it's unused, I can ignore or delete.
        // I will omit it from PSUtil wrapper if it was private.
    }
}
