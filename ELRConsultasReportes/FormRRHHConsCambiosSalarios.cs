﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ELRConsultasReportes
{
    public partial class FormRRHHConsCambiosSalarios : WinControl_ELR_NET.FormPlantillaIndex
    {
        int empleadoId = 0;
        string cedulaRNC = "000000000";

        public FormRRHHConsCambiosSalarios()
        {
            InitializeComponent();
        }

        private void FormRRHHConsCambiosSalarios_Load(object sender, EventArgs e)
        {
            DateTime fecha = DateTime.Now.Date;
            dtpFecha2.Value = fecha;
            dtpFecha1.Value = fecha.AddMonths(-1);

            GetData();
        }

        void GetEmpleado(string codigo = "")
        {
            DataTable DTData = null;
            DataRow fila0 = null;
            string SSQL = "", id = "0", nombre = "(TODOS)";
            ELRMaestros.FormEmpleados frm = new ELRMaestros.FormEmpleados();
            DateTime fechaEntrada;
            try
            {
                codigo = codigo.Trim().Replace('-', ' ');
                cedulaRNC = "000000000";
                fechaEntrada = dtpFecha1.Value;
                if (codigo != "")
                {
                    SSQL = "SELECT TOP 1 Empleado_Id, Nombres, Apellidos, CedulaRNC, Fecha_Entrada FROM VEMPLEADOS";
                    SSQL += " WHERE Cast(Empleado_Id as Varchar) = '" + codigo + "' ";
                    SSQL += " Or REPLACE(CedulaRNC, '-', '') = '" + codigo + "' ";

                    DTData = objDB.GetSQL(SSQL);
                    if (DTData.Rows.Count > 0) fila0 = DTData.Rows[0];
                }
                else
                {
                    frm.empresaID = empresaID;
                    frm.oficinaID = oficinaID;
                    frm.EsAgregar = false;
                    frm.EsModificar = false;
                    frm.esBusqueda = true;
                    frm.EnEjecusion = true;
                    frm.FiltroEstatico = "";
                    frm.Text = "LISTADO DE EMPLEADOS";

                    frm.ShowDialog();

                    if (frm.filaSelecionada != null) fila0 = frm.filaSelecionada;

                }

                if (fila0 != null)
                {
                    id = objUtil.GetAsString("Empleado_Id", fila0);
                    nombre = objUtil.GetAsString("Nombres", fila0);
                    fechaEntrada = objUtil.GetAsDate("Fecha_Entrada", fila0);
                    nombre += " " + objUtil.GetAsString("Apellidos", fila0);
                    cedulaRNC = objUtil.GetAsString("CedulaRNC", fila0);
                }

                dtpFecha1.Value = fechaEntrada;
                txtCodigoEmp.Text = id;
                txtNombre.Text = nombre;
                toolTip1.SetToolTip(txtNombre, nombre);

                GetData();
            }
            catch (Exception ex)
            {

                objUtil.MostrarMensajeError(ex.Message);
            }
        }

        private void btnBuscarEmp_Click(object sender, EventArgs e)
        {
            GetEmpleado();
        }

        private void txtCodigoEmp_KeyDown(object sender, KeyEventArgs e)
        {
            string codigo = txtCodigoEmp.Text.Trim();

            if (e.KeyCode == Keys.F9) GetEmpleado();
            else if (e.KeyCode == Keys.Enter && codigo != "") GetEmpleado(codigo);
        }

        public override void GetData(int opcion = 0)
        {
            int fechaDesde = 0, fechaHasta = 0;
            double total = 0;
            try
            {
                Cursor = Cursors.WaitCursor;

                fechaDesde = objUtil.DateToInt(dtpFecha1.Value);
                fechaHasta = objUtil.DateToInt(dtpFecha2.Value);
                int.TryParse(txtCodigoEmp.Text, out empleadoId);

                objDB.LimpiarFiltros();
                objDB.AddFiltroIgualA("Empresa_Id", empresaID.ToString());
                objDB.AddFiltroIgualA("Tipo", "CAMBIO-SALARIO");
                objDB.AddFiltroIgualA("EsNulo", "0");
                objDB.AddFiltroMayorOIgualA("Fecha", fechaDesde.ToString());
                objDB.AddFiltroMenorOIgualA("Fecha", fechaHasta.ToString());

                if (empleadoId > 0) objDB.AddFiltroIgualA("Empleado_Id", empleadoId.ToString());

                MyData = objDB.GetAll("VRRHH_EMPLEADOS_HISTORICOS", -1, objDB.Filtros);
                DataGridConsulta.AutoGenerateColumns = false;
                DataGridConsulta.DataSource = MyData.DefaultView;
                MostrarCantidadFilas();

                total = GetSum("Aum_Salario");
                lblTotal.Text = total.ToString("N2");

                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {

                objUtil.MostrarMensajeError(ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        public override void Imprimir()
        {
            WinControl_ELR_NET.ELRFormPreviewRDLC frm = new WinControl_ELR_NET.ELRFormPreviewRDLC();
            string pathReporte = "";
            try
            {
                pathReporte = objUtil.GetPathReportRDLC("RptRRHHCambiosSalarios");

                frm.pathReporte = pathReporte;
                frm.DTData = MyData;
                frm.DTOficina = DTOficina;
                frm.oficinaId = oficinaID;
                frm.empresaId = empresaID;
                frm.nombreUsuario = nombreUsuario;

                frm.AddParm("NOMBRE_EMPLEADO", txtNombre.Text);
                frm.AddParm("CEDULA_RNC", cedulaRNC);

                frm.titulo = "CAMBIO DE SALARIO FECHA [ " + objUtil.GetDateAsString(dtpFecha1.Value) + " - " + objUtil.GetDateAsString(dtpFecha2.Value) + "]";

                frm.ShowDialog();
            }
            catch (Exception ex)
            {

                objUtil.MostrarMensajeError(ex.Message);
            }
            finally
            {
                frm.Dispose();
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            string concepto = "";
            int index = 0;
            try
            {
                txtConcepto.Text = "";

                if (MyData == null) return;
                if (MyData.Rows.Count == 0) return;
                if (DataGridConsulta.RowCount <= 0) return;

                index = DataGridConsulta.CurrentRow.Index;
                concepto = DataGridConsulta["Concepto", index].Value.ToString();

                txtConcepto.Text = concepto;

            }
            catch (Exception ex)
            {

                objUtil.MostrarMensajeError(ex.Message);
            }
        }
    }
}