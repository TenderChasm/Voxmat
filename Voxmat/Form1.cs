using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Voxmat.Graphics;

namespace Voxmat
{
    public partial class Form1 : Form
    {
        public Scene Scene;
        public MaterialData Materials;

        public bool IsMouseOnRender;
        public bool IsRightButtonDown;
        public bool IsLeftButtonDown;
        public bool IsMiddleButtonDown;
        public Vector2 MousePos;
        public float WheelDelta;

        public Camera camera;
        public Controls controls;

        public int IDColumnIndex = 2;

        public SettingsForm Settings;

        public Form1()
        {
            Settings = new SettingsForm(this);
            InitializeComponent();
            glControl1.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.glControl1_MouseWheel);
            ModelsGrid.CellValueChanged += new DataGridViewCellEventHandler(ModelsGrid_CellValueChanged);
            ModelsGrid.CurrentCellDirtyStateChanged += new EventHandler(ModelsGrid_CurrentCellDirtyStateChanged);
            ResizeModelsGridColumns();
        }

        public void ImportModel()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string filter = "";
                foreach (string format in VoxelModel.SupportedFormatsInput)
                {
                    filter += format + '|';
                }
                if (filter[filter.Length - 1] == '|')
                {
                    filter = filter.Substring(0, filter.Length - 1);
                }

                openFileDialog.Filter = filter;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;
                    Stream fileStream = openFileDialog.OpenFile();

                    fileName = fileName.Substring(fileName.LastIndexOf('\\') + 1);
                    fileName = fileName.Substring(0, fileName.IndexOf('.'));
                    try
                    {
                        VoxelModel model = new VoxelModel(fileStream, fileName);
                        Scene.AddModel(model);

                        int rowIndex = ModelsGrid.Rows.Add();
                        DataGridViewRow row = ModelsGrid.Rows[rowIndex];
                        row.SetValues(model.Name);
                        row.Tag = model;
                        row.Selected = true;
                        Scene.SelectedModel = model;
                    }
                    catch
                    {
                        MessageBox.Show("The Chosen Model file has an unknown format");
                        return;
                    }
                }
            }

        }

        public void DeleteModel(VoxelModel model)
        {
            foreach(DataGridViewRow row in ModelsGrid.Rows)
            {
                if (row.Tag == model)
                {
                    ModelsGrid.Rows.Remove(row);
                    break;
                }
            }

            foreach(VoxelModel mod in Scene.Models)
            {
                if(mod == model)
                {
                    Scene.Models.Remove(mod);
                    break;
                }
            }

            if(Scene.SelectedModel == model)
            {
                Scene.SelectedModel = null;
            }

        }

        public void ImportMaterials()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string filter = "";
                foreach (string format in MaterialData.SupportedFormatsInput)
                {
                    filter += format + '|';
                }
                if (filter[filter.Length - 1] == '|')
                {
                    filter = filter.Substring(0, filter.Length - 1);
                }

                openFileDialog.Filter = filter;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;
                    Stream fileStream = openFileDialog.OpenFile();

                    try
                    {
                        Materials = new MaterialData(fileStream);
                    } 
                    catch(FormatException e)
                    {
                        MessageBox.Show("Material file has an unknown format");
                        return;
                    }

                    DataGridViewComboBoxColumn IDColumn = (DataGridViewComboBoxColumn)ModelsGrid.Columns[IDColumnIndex];
                    foreach(KeyValuePair<int, string> pair in Materials.Materials)
                    {
                        string str = $"{pair.Key} ({pair.Value})";
                        IDColumn.Items.Add(str);
                    }
                }
            }
        }

        public void SaveScene()
        {
            Stream targetStream;
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.RestoreDirectory = true;
            dialog.FileName = "MyScene";
            dialog.DefaultExt = Scene.sceneFormatName;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if ((targetStream = dialog.OpenFile()) != null)
                {
                    Scene.SaveScene(targetStream);
                    targetStream.Close();
                }
            }
        }

        public void LoadScene()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                string filter = $"Scene main format | *.{Scene.sceneFormatName}";

                openFileDialog.Filter = filter;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog.FileName;
                    Stream fileStream = openFileDialog.OpenFile();

                    try
                    {
                        int oldCount = Scene.Models.Count;
                        Scene.LoadScene(fileStream);
                        int newCount = Scene.Models.Count;

                        for(int i = oldCount; i < newCount; i++)
                        {
                            int rowIndex = ModelsGrid.Rows.Add();
                            DataGridViewRow row = ModelsGrid.Rows[rowIndex];
                            row.SetValues(Scene.Models[i].Name);
                            row.Tag = Scene.Models[i];
                        }

                        ModelsGrid.Rows[newCount - 1].Selected = true;
                        Scene.SelectedModel = (VoxelModel) ModelsGrid.Rows[newCount - 1].Tag;

                    }
                    catch (FileLoadException e)
                    {
                        MessageBox.Show("Main scene file has an unknown format");
                    }

                    string sceneShortName = fileName.Substring(fileName.LastIndexOf("\\"));
                    MessageBox.Show($"Scene {sceneShortName} succefully loaded!");
                }
            }
        }

        public void ExportAll()
        {
            Stream targetStream;
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.RestoreDirectory = true;
            dialog.FileName = "treasure";
            dialog.DefaultExt = Scene.exportFormat;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if ((targetStream = dialog.OpenFile()) != null)
                {
                    Scene.ExportMaterialized(targetStream);
                    targetStream.Close();
                }
            }
        }

        public void ResizeModelsGridColumns()
        {
            int width = ModelsGrid.Width / ModelsGrid.Columns.Count;
            foreach(DataGridViewColumn column in ModelsGrid.Columns)
            {
                column.Width = width;
            }
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            glControl1.MakeCurrent();

            camera = new Camera(new Vector3d(0,0, 30.0f), glControl1.Width / (float)glControl1.Height);
            controls = new Controls(camera, this);

            Scene = new Scene(camera, controls, this, new Vector3i(22, 22, 22));
            SceneXBox.Text = Scene.Size.X.ToString();
            SceneYBox.Text = Scene.Size.Y.ToString();
            SceneZBox.Text = Scene.Size.Z.ToString();
            //Scene.AddModel(model);

            //GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

            timer1.Start();
        }

        public void UpdateCamBoxes()
        {
            Vector3d camPos = Scene.MainCamera.Position;
            if (CamXBox.Text != camPos.X.ToString("N3") && !CamXBox.Focused)
                CamXBox.Text = camPos.X.ToString("N3");
            if (CamYBox.Text != camPos.Y.ToString("N3") && !CamYBox.Focused)
                CamYBox.Text = camPos.Y.ToString("N3");
            if (CamZBox.Text != camPos.Z.ToString("N3") && !CamZBox.Focused)
                CamZBox.Text = camPos.Z.ToString("N3");
        }

        public void UpdateSelectedModelBoxes()
        {
            if (Scene.SelectedModel == null)
                return;

            Vector3i modelPos = Scene.SelectedModel.ScenePosition;
            if (ModelXBox.Text != modelPos.X.ToString() && !ModelXBox.Focused)
                ModelXBox.Text = modelPos.X.ToString();
            if (ModelYBox.Text != modelPos.Y.ToString() && !ModelYBox.Focused)
                ModelYBox.Text = modelPos.Y.ToString();
            if (ModelZBox.Text != modelPos.Z.ToString() && !ModelZBox.Focused)
                ModelZBox.Text = modelPos.Z.ToString();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            //accumulator += timer1.Interval / 1000F;

            Scene.DrawScene();

            glControl1.SwapBuffers();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateCamBoxes();
            UpdateSelectedModelBoxes();
            glControl1.Invalidate();
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            MousePos.X = e.X;
            MousePos.Y = e.Y;
        }

        private void glControl1_MouseEnter(object sender, EventArgs e)
        {
            IsMouseOnRender = true;
        }

        private void glControl1_MouseLeave(object sender, EventArgs e)
        {
            IsMouseOnRender = false;
        }

        private void glControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                IsRightButtonDown = true;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                IsLeftButtonDown = true;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                IsMiddleButtonDown = true;
            }

        }

        private void glControl1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                IsRightButtonDown = false;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                IsLeftButtonDown = false;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                IsMiddleButtonDown = false;
            }

        }

        private void glControl1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            WheelDelta = e.Delta;
        }

        private void importModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportModel();
        }

        private void ModelsGrid_Resize(object sender, EventArgs e)
        {
            ResizeModelsGridColumns();
        }

        private void ModelsGrid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {

            if (ModelsGrid.CurrentCell.ColumnIndex == IDColumnIndex)
            {
                ComboBox combo = e.Control as ComboBox;

                if (combo == null)
                    return;

                combo.DropDownStyle = ComboBoxStyle.DropDown;
            }
        }

        private void saveSceneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ImportMaterials();
        }

        void ModelsGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {

        }

        private void ModelsGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // My combobox column is the second one so I hard coded a 1, flavor to taste
            DataGridViewRow selectedRow = ModelsGrid.Rows[e.RowIndex];

            DataGridViewComboBoxCell cb = (DataGridViewComboBoxCell)selectedRow.Cells[IDColumnIndex];
            if (cb.Value != null)
            {
                AttachMaterialToModel(selectedRow, (string)cb.Value);
                ModelsGrid.Invalidate();
            }
        }

        private void AttachMaterialToModel(DataGridViewRow selectedRow, string value)
        {
            string materialName = ((string)value).Split()[1];
            materialName = materialName.Substring(1, materialName.Length - 2);
            ushort materialID = Convert.ToUInt16(((string)value).Split()[0]);

            selectedRow.Cells[IDColumnIndex - 1].Value = materialName;

            VoxelModel attachedModel = (VoxelModel)selectedRow.Tag;
            attachedModel.Material = materialID;
        }

        private void ModelsGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (ModelsGrid.Rows.Count > 0 && ModelsGrid.SelectedRows.Count > 0)
            {
                DataGridViewRow row = ModelsGrid.SelectedRows[0];
                VoxelModel selectedModel = (VoxelModel)row.Tag;
                Scene.SelectedModel = selectedModel;
            }
        }

        void MoveRow(bool direction)
        {
            int currentRowIndex = ModelsGrid.SelectedRows[0].Index;
            int newRowIndex;

            if (direction)
                newRowIndex = currentRowIndex - 1;
            else
                newRowIndex = currentRowIndex + 1;

            var currentRow = ModelsGrid.Rows[currentRowIndex];
            var rowToReplace = ModelsGrid.Rows[newRowIndex];

            ModelsGrid.Rows.Remove(currentRow);
            ModelsGrid.Rows.Insert(newRowIndex, currentRow);

            currentRow.Selected = true;

            VoxelModel temp = Scene.Models[currentRowIndex];
            Scene.Models[currentRowIndex] = Scene.Models[newRowIndex];
            Scene.Models[newRowIndex] = temp;
        }

        void ProcessKeyInput(KeyEventArgs e)
        {
            if (Scene.SelectedModel != null)
            {
                controls.UnitMoveObject(Scene.SelectedModel, e.KeyCode);
                controls.ControlObject(Scene.SelectedModel, e.KeyCode);

                if (e.KeyCode == Keys.R && ModelsGrid.SelectedRows[0].Index > 0)
                    MoveRow(true);
                if (e.KeyCode == Keys.F && ModelsGrid.SelectedRows[0].Index < ModelsGrid.Rows.Count - 1)
                    MoveRow(false);
            }
        }

        private void glControl1_KeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeyInput(e);
        }

        private void ModelsGrid_KeyDown(object sender, KeyEventArgs e)
        {
            ProcessKeyInput(e);
        }
        private void SceneXBox_TextChanged(object sender, EventArgs e)
        {
            int sceneX;
            bool result = Int32.TryParse(SceneXBox.Text, out sceneX);

            if(result)
            {
                Scene.Size.X = sceneX;
            }
        }

        private void SceneYBox_TextChanged(object sender, EventArgs e)
        {
            int sceneY;
            bool result = Int32.TryParse(SceneYBox.Text, out sceneY);

            if (result)
            {
                Scene.Size.Y = sceneY;
            }
        }

        private void SceneZBox_TextChanged(object sender, EventArgs e)
        {
            int sceneZ;
            bool result = Int32.TryParse(SceneZBox.Text, out sceneZ);

            if (result)
            {
                Scene.Size.Z = sceneZ;
            }
        }

        private void saveSceneToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            SaveScene();
        }

        private void loadSceneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadScene();
        }

        private void uniteAndExportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportAll();
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void CamXBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Scene.MainCamera.Position.X = Convert.ToDouble(CamXBox.Text);
        }

        private void CamYBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Scene.MainCamera.Position.Y = Convert.ToDouble(CamYBox.Text);
        }

        private void CamZBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                Scene.MainCamera.Position.Z = Convert.ToDouble(CamZBox.Text);
        }

        private void ModelXBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && Scene.SelectedModel != null)
                Scene.SelectedModel.ScenePosition = new Vector3i(Convert.ToInt32(ModelXBox.Text),
                            Scene.SelectedModel.ScenePosition.Y, Scene.SelectedModel.ScenePosition.Z);
        }

        private void ModelYBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && Scene.SelectedModel != null)
                Scene.SelectedModel.ScenePosition = new Vector3i(Scene.SelectedModel.ScenePosition.X,
                            Convert.ToInt32(ModelYBox.Text), Scene.SelectedModel.ScenePosition.Z);
        }

        private void ModelZBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && Scene.SelectedModel != null)
                Scene.SelectedModel.ScenePosition = new Vector3i(Scene.SelectedModel.ScenePosition.X,
                            Scene.SelectedModel.ScenePosition.Y, Convert.ToInt32(ModelZBox.Text));
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            Scene.MainCamera.Reset();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void versionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Version 1.1", "By TenderChasm");
        }
    }
}
