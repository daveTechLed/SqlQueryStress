#region

using Microsoft.Data.SqlClient;
using SQLQueryStress.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Forms;

#endregion

namespace SQLQueryStress
{
    public partial class ParamWindow : Form
    {
        //load query defined in the main form
        private readonly string _outerQuery;

        //parameter values from the parameter query defined in this form
        private readonly Dictionary<string, string> _paramValues = new Dictionary<string, string>();

        //Query Stress Settings
        private readonly QueryStressSettings _settings;

        //Variables from the load query
        private string[] _queryVariables;

        public ParamWindow(QueryStressSettings settings, string outerQuery)
        {
            InitializeComponent();

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _outerQuery = outerQuery ?? throw new ArgumentNullException(nameof(outerQuery));

            var sqlControl = elementHost1.Child as SqlControl;
            if (sqlControl != null)
            {
                sqlControl.Text = (string)settings.ParamQuery.Clone();
            }

            columnMapGrid.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            columnMapGrid.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;
            columnMapGrid.Columns[2].SortMode = DataGridViewColumnSortMode.NotSortable;

            columnMapGrid.CurrentCellDirtyStateChanged += columnMapGrid_CurrentCellDirtyStateChanged;
            columnMapGrid.CellClick += columnMapGrid_CellClick;

            if (sqlControl != null && (outerQuery.Length > 0) && (sqlControl.Text.Length > 0))
            {
                getColumnsButton_Click("constructor", null);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Dispose();
        }

        private void columnMapGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (columnMapGrid.CurrentCell != null && columnMapGrid.CurrentCell.ColumnIndex == 2)
            { 
                var editingControl = columnMapGrid.EditingControl as
                    DataGridViewComboBoxEditingControl;
                if (editingControl != null)
                    editingControl.DroppedDown = true;
            }
        }

        private void columnMapGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            //handle changes to the parameter column
            if (columnMapGrid.CurrentCell != null && columnMapGrid.CurrentCell.ColumnIndex == 2)
            {
                var theRow = columnMapGrid.Rows[columnMapGrid.CurrentCell.RowIndex];
                var combo = (DataGridViewComboBoxCell)theRow.Cells[2];

                if (combo.Value != null)
                {
                    var colType = _paramValues[(string)combo.Value];
                    theRow.Cells[1].Value = colType;
                }
                else
                {
                    theRow.Cells[1].Value = string.Empty;
                }

                columnMapGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void database_button_Click(object sender, EventArgs e)
        {
            using var dbSelect = new DatabaseSelect(_settings) { StartPosition = FormStartPosition.CenterParent };
            dbSelect.ShowDialog();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        private void getColumnsButton_Click(object sender, EventArgs e)
        {
            _queryVariables = GetParams();

            SqlDataReader reader = null;

            var dbInfo = _settings.ShareDbSettings ? _settings.MainDbConnectionInfo : _settings.ParamDbConnectionInfo;

            if (!dbInfo.TestConnection())
            {
                MessageBox.Show(Resources.MustSetValidDatabaseConn, Resources.AppTitle);
                return;
            }

            using var conn = new SqlConnection(dbInfo.ConnectionString);
            try
            {
                if (elementHost1.Child is SqlControl sqlControl)
                {
                    using var sqlCommand = new SqlCommand(sqlControl.Text, conn);
                    conn.Open();
                    reader = sqlCommand.ExecuteReader(CommandBehavior.SchemaOnly);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Resources.AppTitle);
            }

            if (reader != null)
            {
                columnMapGrid.Rows.Clear();
                _paramValues.Clear();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    _paramValues.Add(reader.GetName(i), reader.GetDataTypeName(i));
                }

                reader.Dispose();

                foreach (var variable in _queryVariables)
                {
                    var colOrdinal = columnMapGrid.Rows.Add();
                    var row = columnMapGrid.Rows[colOrdinal];
                    row.Cells[0].Value = variable;
                    row.Cells[0].ReadOnly = true;

                    //placeholder for columntype
                    row.Cells[1].Value = string.Empty;
                    row.Cells[1].ReadOnly = true;

                    var combo = new DataGridViewComboBoxCell();

                    combo.Items.Add(string.Empty);

                    bool checkParam = sender is string s &&
                                      s.Equals("constructor", StringComparison.OrdinalIgnoreCase) &&
                                      _settings.ParamMappings.ContainsKey(variable);

                    foreach (var paramName in _paramValues.Keys)
                    {
                        combo.Items.Add(paramName);

                        if (checkParam)
                        {
                            if (_settings.ParamMappings[variable] == paramName)
                            {
                                combo.Value = paramName;
                                row.Cells[1].Value = _paramValues[paramName];
                            }
                        }
                    }

                    row.Cells[2] = combo;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
        private string[] GetParams()
        {
            //Find all SQL variables:
            //'@', preceded by '=', ',', or any white space character
            //then, any "word" character
            //Finally, '=', ',', or any white space, repeated 0 or more times 
            //(in the case of end-of-string, will be 0 times)
            var r = new Regex(@"(?<=[=,\s\(])@\w{1,}(?=[=,\s\)]?)");

            var output = new List<string>();

            foreach (Match m in r.Matches(_outerQuery))
            {
                var lowerVal = m.Value.ToLowerInvariant();
                if (!output.Contains(lowerVal))
                    output.Add(lowerVal);
            }

            if (output.Count == 0)
                MessageBox.Show(Resources.NoVarsWereIdentified, Resources.AppTitle);

            return output.ToArray();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (elementHost1.Child is SqlControl sqlControl)
            {
                _settings.ParamQuery = sqlControl.Text;
            }

            var localParamMappings = new Dictionary<string, string>();
            foreach (DataGridViewRow row in columnMapGrid.Rows)
            {
                if (!string.IsNullOrEmpty((string)row.Cells[2].Value))
                    localParamMappings.Add((string)row.Cells[0].Value, (string)row.Cells[2].Value);
            }

            _settings.ParamMappings = localParamMappings;

            Dispose();
        }
    }
}