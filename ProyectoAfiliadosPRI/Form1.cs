using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProyectoAfiliadosPRI
{
    public partial class Afiliados : Form
    {
        private DataTable tablaDatos = new DataTable();
        private bool modoArchivos = true;
        private string direccionCarpeta = @"\\SOFIAGOMEZ\Users\52624\Desktop\Afiliados";
        public Afiliados()
        {
            InitializeComponent();
            CargarArchivos();

            dtpPrimera.Enabled = cbFiltro.Checked;
            dtpUltima.Enabled = cbFiltro.Checked;
        }
        private void CargarArchivos()
        {
            if (Directory.Exists(direccionCarpeta))
            {
                var archivos = Directory.GetFiles(direccionCarpeta, "*.xlsx");

                cbEstados.Items.Clear();
                foreach (var archivo in archivos)
                {
                   
                    cbEstados.Items.Add(new Archivo
                    {
                        Texto = Path.GetFileName(archivo),
                        Ruta = archivo
                    });
                }
                modoArchivos = true;

            }
        }
        private void cbEstados_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbEstados.SelectedItem == null) return;

            dgvPersonas.Rows.Clear();

            if (modoArchivos)
            {
                var archivoSeleccionado = cbEstados.SelectedItem as Archivo;
                if (archivoSeleccionado == null) return;

                string rutaArchivo = archivoSeleccionado.Ruta;

                Thread hilo = new Thread(() =>
                {

                    using (var libro = new XLWorkbook(rutaArchivo))
                    {
                        var hoja = libro.Worksheet(1);
                        var rango = hoja.RangeUsed();

                        DataTable dt = new DataTable();


                        var encabezados = rango.FirstRowUsed().Cells().Select(c => c.Value.ToString()).Distinct() .ToList();

                        foreach (var encabezado in encabezados)
                            dt.Columns.Add(encabezado);

                        foreach (var fila in rango.RowsUsed().Skip(1))
                        {
                            var nuevaFila = dt.NewRow();
                            for (int i = 0; i < encabezados.Count; i++)
                                nuevaFila[i] = fila.Cell(i + 1).Value.ToString();
                            dt.Rows.Add(nuevaFila);
                        }
                        tablaDatos = dt;

                        string.Join(", ", tablaDatos.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

                    }



                    dgvPersonas.Invoke(new Action(() =>
                {
                     dgvPersonas.Columns.Clear();
                    foreach (DataColumn col in tablaDatos.Columns)
                    {
                        dgvPersonas.Columns.Add(col.ColumnName, col.ColumnName);
                    }
                    if (tablaDatos.Columns.Contains("ENTIDAD"))
                    {
                        var estados = tablaDatos.AsEnumerable().Select(r => r["ENTIDAD"].ToString().Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToArray();


                    }
                    if (tablaDatos.Columns.Contains("MUNICIPIO"))
                    {
                        var municipios = tablaDatos.AsEnumerable()
                            .Select(r => r["MUNICIPIO"].ToString().Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct()
                            .OrderBy(s => s)
                            .ToArray();

                        cbMunicipios.Items.Clear();
                        cbMunicipios.Items.Add("Todos");
                        cbMunicipios.Items.AddRange(municipios);
                    }


                    FiltrarDatos();

                }));
                });

                hilo.IsBackground = true;
                hilo.Start();
            }
            else
            {
                
                FiltrarDatos();
            }
        }

        private void FiltrarDatos()
        {
            var filasFiltradas = tablaDatos.AsEnumerable();

            if (cbMunicipios.SelectedItem != null && cbMunicipios.SelectedItem.ToString() != "Todos")
            {
                string municipioSeleccionado = cbMunicipios.SelectedItem.ToString();
                filasFiltradas = filasFiltradas.Where(r => r["MUNICIPIO"].ToString().Trim() == municipioSeleccionado);
            }

            dgvPersonas.Rows.Clear();

            var resultados = filasFiltradas.ToList();

            foreach (var row in resultados)
            {
                int rowIndex = dgvPersonas.Rows.Add();

                for (int i = 0; i < row.ItemArray.Length; i++)
                {
                    dgvPersonas.Rows[rowIndex].Cells[i].Value = row[i];
                }
            }

            ActualizarTotal();
        }

        private void ActualizarTotal()
        {
            int visibles = dgvPersonas.Rows.Cast<DataGridViewRow>().Count(r => r.Visible && !r.IsNewRow);
            lblNumAfiliados.Text = $"Mostrando: {visibles} registros";
        }

        private void cbFiltro_CheckedChanged(object sender, EventArgs e)
        {
            dtpPrimera.Enabled = cbFiltro.Checked;
            dtpUltima.Enabled = cbFiltro.Checked;

            if (cbFiltro.Checked == true)
            {
                PFecha.Visible = true;
            }
            else
            {
                PFecha.Visible = false;
            }


        }

        private void cbMunicipios_SelectedIndexChanged(object sender, EventArgs e)
        {
            FiltrarDatos();
        }
        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();

        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dgvPersonas.Rows.Clear();
            tablaDatos.Clear();

            CargarArchivos();

            cbEstados.SelectedIndex = -1;
            cbEstados.Text = string.Empty;

            cbMunicipios.Items.Clear();
            cbMunicipios.Text = string.Empty;

            cbFiltro.Checked = false;
            
            dtpPrimera.Value = DateTime.Now;
            dtpUltima.Value = DateTime.Now;

            lblNumAfiliados.Text = " Total de Afiliados:";
        }

        private void btnFiltrarFecha_Click(object sender, EventArgs e)
        {
            string nombreColumna = "FECHA_AFILIACION";

            if (!tablaDatos.Columns.Contains(nombreColumna))
            {
                MessageBox.Show($"No se encontró la columna '{nombreColumna}'");
                return;
            }

            DateTime inicio = dtpPrimera.Value.Date;
            DateTime fin = dtpUltima.Value.Date;

            try
            {
                var filasFiltradas = tablaDatos.AsEnumerable().Where(r =>
                {
                    DateTime fecha;
                    if (DateTime.TryParse(r[nombreColumna].ToString(), out fecha))
                    {
                        return fecha.Date >= inicio && fecha.Date <= fin;
                    }
                    return false;
                });

                dgvPersonas.Rows.Clear();
                foreach (var row in filasFiltradas)
                {
                    int rowIndex = dgvPersonas.Rows.Add();
                    for (int i = 0; i < row.ItemArray.Length; i++)
                    {
                        dgvPersonas.Rows[rowIndex].Cells[i].Value = row[i];
                    }
                }

                ActualizarTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al filtrar por fecha: " + ex.Message);
            }
        }

      
    }
}
