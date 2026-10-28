using System;
using System.Drawing;
using System.Windows.Forms;
using FractalPlatform.Client.UI.DOM.Controls;
using FractalPlatform.Database.Dimensions.Language;
using FractalPlatform.Database.Engine;

namespace FractalPlatform.Sandbox.Controls
{
	public class BaseControl : Panel
	{
		private LanguageDimension _languageDimension;
		private ToolTip _validationToolTip; // store as field

		public MainForm ParentForm { get; }

		public DOMControl DOMControl { get; }

		public BaseControl(MainForm parentForm, DOMControl domControl)
		{
			ParentForm = parentForm;

			DOMControl = domControl;

			_languageDimension = domControl.ParentForm.GetLanguageDimension();

			CreateContextMenu();
		}

		protected string GetLocalizedValue(string value)
		{
			return _languageDimension.GetLocalizedValue(DOMControl.ParentForm.Context, value);
		}

		protected void SetValidatonStyle()
		{
			if (DOMControl.HasValidationError)
			{
				// Dispose previous tooltip to prevent leak
				_validationToolTip?.Dispose();
				_validationToolTip = new ToolTip();

				ForeColor = Color.Red;

				_validationToolTip.AutoPopDelay = 3000;
				_validationToolTip.InitialDelay = 500;
				_validationToolTip.ReshowDelay = 300;
				_validationToolTip.ShowAlways = true;

				var message = QueryHelper.Format(DOMControl.ParentForm.Context,
												 DOMControl.ValidationError.Message,
												 DOMControl.ValidationError.Params);

				_validationToolTip.SetToolTip(this, message);

				bool hasLabel = false;

				foreach (Control control in this.Controls)
				{
					if (control is Label)
					{
						hasLabel = true;
					}
				}

				foreach (Control control in this.Controls)
				{
					if (!hasLabel || control is Label)
					{
						control.ForeColor = Color.Red;

						_validationToolTip.SetToolTip(control, message);
					}
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_validationToolTip?.Dispose();
			}
			base.Dispose(disposing);
		}

		protected virtual void CreateContextMenu()
		{
			var contextMenu = DOMControl.ContextMenu;

			if (contextMenu != null &&
				contextMenu.Count > 0)
			{
				var contextMenuStrip = new ContextMenuStrip();

				foreach (var contextMenuItem in contextMenu)
				{
					var menuItem = new ToolStripMenuItem(contextMenuItem.Text);

					menuItem.Tag = contextMenuItem.Action;

					menuItem.Click += ContextMenuItem_Click;

					contextMenuStrip.Items.Add(menuItem);
				}

				this.ContextMenuStrip = contextMenuStrip;
			}
		}

		protected virtual void ContextMenuItem_Click(object sender, EventArgs e)
		{
			var contextMenuItem = (ToolStripMenuItem)sender;

			var action = (string)contextMenuItem.Tag;

			DOMControl.OnMenuClick(action);

			ParentForm.RefreshForm();
		}
	}
}
