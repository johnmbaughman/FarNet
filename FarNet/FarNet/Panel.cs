﻿
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.Collections;
using System.Collections.Generic;

namespace FarNet
{
	/// <summary>
	/// Module panel. Create it or a derived class instance directly,
	/// set properties, add event handlers and open it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Exposed as <see cref="IFar.Panel"/> and <see cref="IFar.Panel2"/>.
	/// </para>
	/// <para>
	/// Properties of this class are requested by the core quite frequently.
	/// For better performance the core caches internal representation of these data.
	/// After opening a panel it is recommended to avoid excessive modifications of data.
	/// </para>
	/// <para>
	/// Some settings often configured before opening:
	/// <see cref="Title"/>,
	/// <see cref="SortMode"/>,
	/// <see cref="ViewMode"/>,
	/// <see cref="SetPlan"/>.
	/// </para>
	/// <para>
	/// After opening changing of panel state properties normally does not cause immediate visual effects.
	/// When all changes are done call one of the redraw methods in order to make the changes shown.
	/// </para>
	/// <para>
	/// Most of <c>UI*</c> methods should not be called directly, they are called by the core on user interaction.
	/// Many of them can be overriden in derived classes but this should be done only if explorer methods are not enough.
	/// </para>
	/// <para>
	/// All <c>Works*</c> members are for internal use only.
	/// </para>
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
	public partial class Panel : IPanel
	{
		/// <summary>
		/// Gets or sets the flag telling that files should be refreshed, reloaded, etc.
		/// </summary>
		/// <remarks>
		/// The flag is set by the core on opening a panel and when a user presses [CtrlR] or [PgDn]/[PgUp] on paging.
		/// Modules also can set it after some panel or data changes before calling the <see cref="Update"/>.
		/// The core drops the flag automatically after panel updates.
		/// <para>
		/// The flag is passed in <see cref="GetFilesEventArgs"/> and used by explorers that cache their data
		/// but still allow them to be refreshed (e.g. on [CtrlR]) or even completely changed (e.g. on paging).
		/// </para>
		/// </remarks>
		public bool NeedsNewFiles { get { return _NeedsNewFiles; } set { _NeedsNewFiles = value; } }
		bool _NeedsNewFiles = true;
		/// <summary>
		/// Gets or sets the maximum number of files for paging. The default is 0, paging is not used.
		/// </summary>
		public int PageLimit { get; set; }
		/// <summary>
		/// Gets or sets the number of files to skip on paging.
		/// </summary>
		public int PageOffset { get; set; }
		readonly Works.IPanelWorks _Panel;
		Panel _Parent;
		Panel _Child;
		/// <summary>
		/// New module panel with its file explorer.
		/// </summary>
		/// <param name="explorer">The panel explorer.</param>
		public Panel(Explorer explorer)
		{
			if (explorer == null) throw new ArgumentNullException("explorer");
			_Panel = Far.Api.WorksPanel(this, explorer);
		}
		/// <summary>
		/// Gets the current file explorer.
		/// </summary>
		/// <remarks>
		/// An explorer is set on creation and it cannot be changed directly.
		/// But the panel should not assume that its explorer is the same,
		/// the core normally changes panel explorers on navigation.
		/// See <see cref="ExplorerEntered"/>.
		/// </remarks>
		public Explorer Explorer { get { return _Panel.MyExplorer; } }
		/// <summary>
		/// Navigates to a virtual file system location specified by the explorer and updates the panel.
		/// </summary>
		/// <param name="explorer">The target explorer.</param>
		/// <remarks>
		/// The target explorer must have the same <see cref="FarNet.Explorer.TypeId"/> as the current one.
		/// </remarks>
		public virtual void Navigate(Explorer explorer)
		{
			if (explorer == null) throw new ArgumentNullException("explorer");
			if (explorer.TypeId != Explorer.TypeId) throw new ArgumentException("New explorer is not compatible with the current.");

			_Panel.Navigate(explorer);
		}
		///
		public Works.IPanelWorks WorksPanel { get { return _Panel; } }
		/// <include file='doc.xml' path='doc/Data/*'/>
		public Hashtable Data { get { return _Data ?? (_Data = new Hashtable()); } }
		Hashtable _Data;
		/// <summary>
		/// Gets the child panel.
		/// </summary>
		public Panel Child { get { return _Child; } }
		/// <summary>
		/// Gets the parent panel.
		/// </summary>
		/// <remarks>
		/// The parent panel is null if this panel is not a child panel.
		/// </remarks>
		public Panel Parent { get { return _Parent; } }
		/// <summary>
		/// Gets the default panel title to be set on show.
		/// </summary>
		protected virtual string DefaultTitle { get { return GetType().Name; } }
		/// <summary>
		/// Gets or sets the panel header.
		/// </summary>
		public string Title { get { return _Panel.Title; } set { _Panel.Title = value; } }
		// Opening worker, including a posted step
		void Open(object sender, EventArgs e)
		{
			//_171024_175829 inherit explorer location if there is no custom (#13)
			if (_Panel.CurrentLocation == null)
			{
				var location = Explorer.Location;
				if (location.Length > 0)
					_Panel.CurrentLocation = location;
				else
					_Panel.CurrentLocation = "*";
			}

			// the first update
			Explorer.EnterPanel(this);

			// go
			if (_Parent == null)
				_Panel.Open();
			else
				_Panel.OpenReplace(_Parent); //???? is it correct to close the parent? should I better deny tricky openings?
		}
		/// <summary>
		/// Tells to open the panel when the core gets control.
		/// </summary>
		/// <remarks>
		/// Normally a panel should be opened when a module is called from panels window
		/// (command line, disk menu or plugins menu in panels). If panels window cannot
		/// be set current, this method fails.
		/// <para>
		/// Other possible reasons of failure:
		/// *) another panel has been already posted for opening;
		/// *) the module is not called for opening, e.g. it is called to process events.
		/// </para>
		/// <para>
		/// It is recommended to call this as soon as possible and only then to configure the panel and other data.
		/// Technically this method only tries to post the panel for opening and may fail due to the reasons above.
		/// Early call and failure avoids jobs for nothing.
		/// </para>
		/// </remarks>
		/// <seealso cref="Open(bool)"/>
		/// <seealso cref="OpenChild"/>
		public virtual void Open()
		{
			// done?
			if (IsOpened)
				return;

			// set the title to default
			if (string.IsNullOrEmpty(Title))
				Title = DefaultTitle;

			// | open from panels
			WindowKind wt = Far.Api.Window.Kind;
			if (wt == WindowKind.Panels)
			{
				Open(null, null);
				return;
			}

			// 090623 PostJob may not work from the editor, for example, see "... because a
			// module is not called for opening". In contrast, PostStep calls via the menu
			// where a panel is opened fine.

			// 180913 Make it two steps: (1) set panels, (2) open panel. One step used to
			// work but stopped. Far issue or not, let's use more reliable two step way.

			Far.Api.PostSteps(
				new Action[] {
					delegate
					{
						// #7 make switching async, SetCurrentAt does not work for user screen
						try
						{
							Far.Api.Window.SetCurrentAt(-1);
						}
						catch (InvalidOperationException ex)
						{
							throw new ModuleException("Cannot open a panel because panels cannot be set current.", ex);
						}
					},
					delegate
					{
						Open(null, null);
					}
				}
			);
		}
		/// <summary>
		/// Calls <see cref="Open()"/> or <see cref="OpenChild"/> depending on the parameter.
		/// </summary>
		/// <param name="child">Tells to open the panel as a child of the active module panel, if any.</param>
		public void Open(bool child)
		{
			if (child)
				OpenChild(null);
			else
				Open();
		}
		/// <summary>
		/// Opens this panel as a child of the parent panel.
		/// </summary>
		/// <param name="parent">The opend parent panel. Null tells to use the active module panel, if any.</param>
		/// <remarks>
		/// When this panel is opened as a child of the parent panel, the parent is hidden, not closed.
		/// When the child closes itself later then the parent is shown again and its state is restored.
		/// </remarks>
		public void OpenChild(Panel parent)
		{
			// resolve 'null' parent
			if (parent == null)
			{
				// try to use the active as parent; do not use the passive, show as normal
				parent = Far.Api.Panel as Panel;
				if (parent == null)
				{
					// go
					Open();
					return;
				}
			}

			// sanity check
			if (IsOpened || _Parent != null)
				throw new InvalidOperationException();

			// setup/fail
			if (!CanOpenAsChild(parent))
				return;

			// link
			_Parent = parent;
			_Parent._Child = this;

			// begin
			_Parent.SaveState();

			// go
			Open();
		}
		/// <summary>
		/// It is called from <see cref="OpenChild"/> before linking the panels together.
		/// </summary>
		/// <param name="parent">The panel which is about to be parent.</param>
		/// <returns>False to cancel opening.</returns>
		/// <remarks>
		/// This method is designed for cases when the child opening depends on the parent.
		/// When this methods is called the parent is the active panel and its data can
		/// be used as usual. The panels are not yet linked together, use the parameter
		/// in order to access the parent.
		/// <para>
		/// If it returns false or throws then the parent panel remains opened.
		/// </para>
		/// </remarks>
		protected virtual bool CanOpenAsChild(Panel parent) { return true; }
		/// <summary>
		/// Gets the opposite opened module panel, the target for dual operations, or null.
		/// </summary>
		public Panel TargetPanel { get { return _Panel.TargetPanel; } }
		/// <summary>
		/// Saves the panel state.
		/// </summary>
		/// <remarks>
		/// It is called when the panel is about to be offline (pushed or replaced by a child panel).
		/// The panel UI state is saved by the core (view and sort modes, etc.).
		/// The base method posts the current file to be restored as current.
		/// It is important to have the proper <see cref="FarNet.Explorer.FileComparer"/>.
		/// </remarks>
		protected virtual void SaveState()
		{
			FarFile file = CurrentFile;
			if (file != null)
				PostFile(file);
		}
		/// <summary>
		/// Closes this child panel and opens the parent panel if both panels are ready.
		/// </summary>
		/// <remarks>
		/// The method only tries to close the child panel.
		/// The panel is not closed if <see cref="CanClose"/> or <see cref="CanCloseChild"/> gets false.
		/// </remarks>
		public void CloseChild()
		{
			if (_Parent == null)
				throw new InvalidOperationException("The panel is not child.");

			// ask child
			if (!CanClose())
				return;

			// ask parent
			if (!_Parent.CanCloseChild())
				return;

			try
			{
				// clean
				UIClosed();
			}
			finally
			{
				// open parent
				_Parent._Panel.OpenReplace(this);

				// unlink
				_Parent._Child = null;
				_Parent = null;
			}
		}
		/// <summary>
		/// Can the panel close now?
		/// </summary>
		/// <remarks>
		/// It can be called from a child; in this case the panel is offline.
		/// </remarks>
		protected virtual bool CanClose() { return true; }
		/// <summary>
		/// Can the parent panel close its child?
		/// </summary>
		protected virtual bool CanCloseChild() { return true; }
		/// <summary>
		/// Gets the list of user objects to be disposed when the panel is closed.
		/// </summary>
		/// <remarks>
		/// It is mostly designed for PowerShell scripts and not recommended for modules.
		/// </remarks>
		[Obsolete("PowerShell scripts helper.")]
		public IList<IDisposable> Garbage { get { return _Garbage ?? (_Garbage = new List<IDisposable>()); } }
		List<IDisposable> _Garbage;
		/// <summary>
		/// Saves the panel data.
		/// </summary>
		/// <returns>True if there is no more data to save.</returns>
		public virtual bool SaveData()
		{
			return true;
		}
		#region Work Properties
		/// <summary>
		/// Gets <see cref="IFar.CurrentDirectory"/> saved when the panel starts.
		/// </summary>
		public string StartDirectory { get { return _Panel.StartDirectory; } }
		/// <summary>
		/// Tells how to add the dots item. See also <see cref="DotsDescription"/>.
		/// </summary>
		public PanelDotsMode DotsMode { get; set; }
		/// <summary>
		/// Gets or sets the dots item item description.
		/// </summary>
		/// <remarks>
		/// This text is used and shown only if the dots item is added, see <see cref="DotsMode"/>.
		/// </remarks>
		public string DotsDescription { get; set; }
		/// <summary>
		/// Gets true if the panel is opened.
		/// </summary>
		public bool IsOpened { get { return _Panel.IsOpened; } }
		/// <summary>
		/// Gets true if the panel is pushed.
		/// </summary>
		public bool IsPushed { get { return _Panel.IsPushed; } }
		/// <summary>
		/// Gets or sets the panel type ID.
		/// </summary>
		/// <remarks>
		/// This property is optionally set once, normally by a creator.
		/// It is used for distinguishing panel types when a class type is not enough.
		/// </remarks>
		/// <seealso cref="IFar.Panels(Guid)"/>
		public Guid TypeId { get; set; }
		#endregion
		#region Work Methods
		void Post(ExplorerEventArgs args)
		{
			if (args.PostData != null) PostData(args.PostData);
			if (args.PostFile != null) PostFile(args.PostFile);
			if (args.PostName != null) PostName(args.PostName);
		}
		/// <summary>
		/// Posts the <see cref="FarFile.Data"/> to be used to find a file and set it current on redrawing.
		/// </summary>
		/// <param name="data">The file data to be found on redrawing.</param>
		public void PostData(object data) { _Panel.PostData(data); }
		/// <summary>
		/// Posts the file to be found and set current on redrawing.
		/// </summary>
		/// <param name="file">The file to be found on redrawing.</param>
		/// <remarks>
		/// The posted file is ignored if <see cref="PostData"/> or <see cref="PostName"/> were called.
		/// The <see cref="FarNet.Explorer.FileComparer"/> is used in order to find the file.
		/// </remarks>
		public void PostFile(FarFile file) { _Panel.PostFile(file); }
		/// <summary>
		/// Posts the file name to be used to find a file and set it current on redrawing.
		/// </summary>
		/// <param name="name">The file name to found on redrawing.</param>
		public void PostName(string name) { _Panel.PostName(name); }
		#endregion
		#region Settable modes
		/// <summary>
		/// Gets or sets the directories first sort flag.
		/// </summary>
		public bool DirectoriesFirst { get { return _Panel.DirectoriesFirst; } set { _Panel.DirectoriesFirst = value; } }
		#endregion
		#region Read only modes (at least for open)
		/// <summary>
		/// Gets the file highlighting flag.
		/// </summary>
		public bool Highlight { get { return _Panel.Highlight; } }
		/// <summary>
		/// Tells that the panel items represent the real file system.
		/// Set it before opening.
		/// </summary>
		/// <remarks>
		/// If this flag is set then panel item names are related to real file system names.
		/// </remarks>
		public bool RealNames { get { return _Panel.RealNames; } set { _Panel.RealNames = value; } }
		/// <summary>
		/// Gets the show selected files first flag.
		/// </summary>
		public bool SelectedFirst { get { return _Panel.SelectedFirst; } }
		/// <summary>
		/// Gets true if hidden and system files are shown.
		/// </summary>
		public bool ShowHidden { get { return _Panel.ShowHidden; } }
		/// <summary>
		/// Gets the use sort groups flags.
		/// Set it before opening.
		/// </summary>
		public bool UseSortGroups { get { return _Panel.UseSortGroups; } set { _Panel.UseSortGroups = value; } }
		#endregion
		#region Properties
		/// <summary>
		/// Gets the current file.
		/// </summary>
		public FarFile CurrentFile { get { return _Panel.CurrentFile; } }
		/// <summary>
		/// Gets the current file index in the <see cref="ShownList"/> files.
		/// </summary>
		/// <remarks>
		/// This is the index of the current file in the <see cref="ShownList"/> files.
		/// It is not directly related to other panel file collections.
		/// </remarks>
		public int CurrentIndex { get { return _Panel.CurrentIndex; } }
		/// <summary>
		/// Gets the current frame: current and top file indexes.
		/// </summary>
		public Point Frame { get { return _Panel.Frame; } }
		/// <summary>
		/// Gets true if the panel is active.
		/// </summary>
		/// <seealso cref="SetActive"/>
		public bool IsActive { get { return _Panel.IsActive; } }
		/// <summary>
		/// Gets true if the panel is the left panel.
		/// </summary>
		public bool IsLeft { get { return _Panel.IsLeft; } }
		/// <summary>
		/// Gets true if the panel is designed for navigation by paths.
		/// </summary>
		public bool IsNavigation { get { return _Panel.IsNavigation; } }
		/// <summary>
		/// Gets true always.
		/// </summary>
		public bool IsPlugin { get { return true; } }
		/// <summary>
		/// Gets or sets visibility of the panel.
		/// </summary>
		/// <remarks>
		/// Setting takes effect only when Far gets control.
		/// </remarks>
		public bool IsVisible { get { return _Panel.IsVisible; } set { _Panel.IsVisible = value; } }
		/// <summary>
		/// Gets file kind always.
		/// </summary>
		public PanelKind Kind { get { return PanelKind.File; } }
		/// <include file='doc.xml' path='doc/CurrentDirectory/*'/>
		public string CurrentDirectory { get { return _Panel.CurrentDirectory; } set { _Panel.CurrentDirectory = value; } }
		/// <summary>
		/// Gets or sets the path which is or going to be the <see cref="CurrentDirectory"/>.
		/// </summary>
		/// <remarks>
		/// It should be set only when the directory is not the same as the explorer location, this is a rare case.
		/// If it is empty then the core closes the panel when [Enter] is pressed on the dots item.
		/// </remarks>
		public string CurrentLocation { get { return _Panel.CurrentLocation; } set { _Panel.CurrentLocation = value; } }
		/// <summary>
		/// Gets all selected panel files at once or the current file if none is selected.
		/// </summary>
		/// <remarks>
		/// In contrast to <see cref="SelectedList"/> this list is a snapshot of files,
		/// it can be used even after changes in the panel.
		/// </remarks>
		public IList<FarFile> SelectedFiles { get { return _Panel.SelectedFiles; } }
		/// <summary>
		/// Gets all selected panel files or the current file if none is selected.
		/// </summary>
		/// <remarks>
		/// In contrast to <see cref="SelectedFiles"/> you must not change panel items while using this list.
		/// </remarks>
		public IList<FarFile> SelectedList { get { return _Panel.SelectedList; } }
		/// <summary>
		/// Gets all shown panel files at once. File ".." is excluded.
		/// </summary>
		/// <remarks>
		/// In contrast to <see cref="ShownList"/> this list is a snapshot of all files,
		/// it can be used even after changes in the panel.
		/// </remarks>
		public IList<FarFile> ShownFiles { get { return _Panel.ShownFiles; } }
		/// <summary>
		/// Gets all shown panel files including "..".
		/// </summary>
		/// <remarks>
		/// In contrast to <see cref="ShownFiles"/> you must not change panel items while using this list.
		/// The current file index in this list is <see cref="CurrentIndex"/>.
		/// </remarks>
		public IList<FarFile> ShownList { get { return _Panel.ShownList; } }
		/// <summary>
		/// Gets the first visible file index.
		/// </summary>
		public int TopIndex { get { return _Panel.TopIndex; } }
		/// <summary>
		/// Gets or sets the panel sort mode. It works before and after opening.
		/// </summary>
		public PanelSortMode SortMode { get { return _Panel.SortMode; } set { _Panel.SortMode = value; } }
		/// <summary>
		/// Gets or sets the panel view mode. It works before and after opening.
		/// </summary>
		public PanelViewMode ViewMode { get { return _Panel.ViewMode; } set { _Panel.ViewMode = value; } }
		/// <include file='doc.xml' path='doc/ViewPlan/*'/>
		public PanelPlan ViewPlan { get { return _Panel.ViewPlan; } }
		/// <summary>
		/// Gets the panel window position.
		/// </summary>
		public Place Window { get { return _Panel.Window; } }
		#endregion
		#region Core
		/// <summary>
		/// Tells to convert timestamps to FAT format for the Compare folders operation.
		/// </summary>
		/// <remarks>
		/// Set this flag if the panel file system doesn't provide time accuracy necessary for standard comparison operations.
		/// </remarks>
		public bool CompareFatTime { get { return _Panel.CompareFatTime; } set { _Panel.CompareFatTime = value; } }
		/// <summary>
		/// Tells to show file names using original case regardless of Far settings.
		/// </summary>
		public bool PreserveCase { get { return _Panel.PreserveCase; } set { _Panel.PreserveCase = value; } }
		/// <summary>
		/// Tells that folders are selected regardless of the core settings.
		/// </summary>
		public bool RawSelection { get { return _Panel.RawSelection; } set { _Panel.RawSelection = value; } }
		/// <summary>
		/// Tells to use the core method instead of explorer if <see cref="RealNames"/> is true.
		/// </summary>
		public bool RealNamesDeleteFiles { get { return _Panel.RealNamesDeleteFiles; } set { _Panel.RealNamesDeleteFiles = value; } } //????? to explorer?
																																	  /// <summary>
																																	  /// Tells to use the core method instead of explorer if <see cref="RealNames"/> is true.
																																	  /// </summary>
		public bool RealNamesExportFiles { get { return _Panel.RealNamesExportFiles; } set { _Panel.RealNamesExportFiles = value; } }
		/// <summary>
		/// Tells to use the core method instead of explorer if <see cref="RealNames"/> is true.
		/// </summary>
		public bool RealNamesImportFiles { get { return _Panel.RealNamesImportFiles; } set { _Panel.RealNamesImportFiles = value; } }
		/// <summary>
		/// Tells to use the core method instead of explorer if <see cref="RealNames"/> is true.
		/// </summary>
		public bool RealNamesMakeDirectory { get { return _Panel.RealNamesMakeDirectory; } set { _Panel.RealNamesMakeDirectory = value; } }
		/// <summary>
		/// Tells to show file names right-aligned by default in all panel display modes.
		/// </summary>
		public bool RightAligned { get { return _Panel.RightAligned; } set { _Panel.RightAligned = value; } }
		/// <summary>
		/// Tells to show file names without paths by default.
		/// </summary>
		public bool ShowNamesOnly { get { return _Panel.ShowNamesOnly; } set { _Panel.ShowNamesOnly = value; } }
		/// <summary>
		/// Tells to disable filters in the panel.
		/// </summary>
		public bool NoFilter { get { return _Panel.NoFilter; } set { _Panel.NoFilter = value; } }
		/// <summary>
		/// Gets or sets the highlighting mode.
		/// </summary>
		public PanelHighlighting Highlighting { get { return _Panel.Highlighting; } set { _Panel.Highlighting = value; } }
		#endregion
		#region Methods
		/// <summary>
		/// Redraws the panel. Normally you should call it after changes to make them visible.
		/// </summary>
		public void Redraw() { _Panel.Redraw(); }
		/// <summary>
		/// Redraws the panel and sets the current and\or the first visible item.
		/// If both arguments are negative, result is the same as per <see cref="Redraw()"/>
		/// </summary>
		/// <param name="current">Index of the current panel item.</param>
		/// <param name="top">Index of the first visible panel item.</param>
		public void Redraw(int current, int top) { _Panel.Redraw(current, top); }
		/// <summary>
		/// Updates panel contents.
		/// </summary>
		/// <param name="keepSelection">Keep the current selection.</param>
		public void Update(bool keepSelection) { _Panel.Update(keepSelection); }
		/// <summary>
		/// Closes the module panel and all parents and opens the original file panel.
		/// </summary>
		/// <remarks>
		/// Mantis 1114: the current original panel item depends on the current plugin panel item on closing.
		/// It is resolved for module panels, the original current and even selected items should be restored.
		/// </remarks>
		public void Close() { _Panel.Close(); } // _090321_210416
												/// <summary>
												/// Closes the module panel and all parents and opens the original file panel with the specified path.
												/// </summary>
												/// <param name="path">
												/// Name of the directory that will be set in the panel after closing the panel (or {null|empty}).
												/// If the path doesn't exist the core shows an error message box always.
												/// </param>
		public void Close(string path) { _Panel.Close(path); }
		/// <summary>
		/// Sets the specified item current by name, if it exists.
		/// </summary>
		/// <param name="name">Name or alternative name of an item to be set current.</param>
		/// <remarks>
		/// If the item does not exist or it is hidden and the panel does not show hidden items
		/// then just nothing happens, it is not an error.
		/// </remarks>
		/// <seealso cref="CurrentDirectory"/>
		/// <seealso cref="GoToPath"/>
		/// <seealso cref="GoToName(string, bool)"/>
		public void GoToName(string name) { _Panel.GoToName(name); }
		/// <summary>
		/// Sets the specified item current by name and optionally fails if it is not shown.
		/// </summary>
		/// <returns>True if a file is found.</returns>
		/// <param name="name">Name or alternative name of a file to be set current.</param>
		/// <param name="fail">Fail mode: to throw if a file is not shown.</param>
		/// <seealso cref="CurrentDirectory"/>
		/// <seealso cref="GoToName(string)"/>
		/// <seealso cref="GoToPath"/>
		public bool GoToName(string name, bool fail) { return _Panel.GoToName(name, fail); }
		/// <summary>
		/// Opens the parent directory of a specified item and sets the item current in the panel.
		/// </summary>
		/// <param name="path">Path of an item to be set current.</param>
		/// <remarks>
		/// If the path is not valid or the parent directory does not exist an exception is thrown.
		/// Otherwise the directory of the item is opened on the panel. If the item does not exist
		/// or it is hidden and the panel does not show hidden items it is fine, in this case the
		/// top panel item is set current after the call. Otherwise the requested item is set as
		/// the current.
		/// </remarks>
		/// <seealso cref="CurrentDirectory"/>
		/// <seealso cref="GoToName(string)"/>
		public void GoToPath(string path) { _Panel.GoToPath(path); }
		/// <summary>
		/// Selects all shown items.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Redraw()"/> after that.
		/// </remarks>
		public void SelectAll() { _Panel.SelectAll(); }
		/// <summary>
		/// Unselects all shown items.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Redraw()"/> after that.
		/// </remarks>
		public void UnselectAll() { _Panel.UnselectAll(); }
		/// <summary>
		/// Selects shown items by their indexes.
		/// </summary>
		/// <param name="indexes">Indexes of items to be selected. Null is OK.</param>
		/// <remarks>
		/// Call <see cref="Redraw()"/> after that.
		/// </remarks>
		public void SelectAt(int[] indexes) { _Panel.SelectAt(indexes); }
		/// <summary>
		/// Unselects shown items by their indexes. See <see cref="Redraw()"/>.
		/// </summary>
		/// <param name="indexes">Indexes of items to be unselected. Null os OK.</param>
		/// <remarks>
		/// Call <see cref="Redraw()"/> after that.
		/// </remarks>
		public void UnselectAt(int[] indexes) { _Panel.UnselectAt(indexes); }
		/// <include file='doc.xml' path='doc/SelectNames/*'/>
		public void SelectNames(IEnumerable names) { _Panel.SelectNames(names); }
		/// <include file='doc.xml' path='doc/UnselectNames/*'/>
		public void UnselectNames(IEnumerable names) { _Panel.UnselectNames(names); }
		/// <summary>
		/// Pushes or puts the panel to the internal panel shelve.
		/// </summary>
		public void Push() { _Panel.Push(); }
		/// <summary>
		/// Gets indexes of selected items.
		/// </summary>
		/// <remarks>
		/// The indexes are valid only for the <see cref="ShownList"/> items.
		/// Unlike the <see cref="SelectedFiles"/> or <see cref="SelectedList"/> this list is empty if none is selected.
		/// </remarks>
		public int[] SelectedIndexes() { return _Panel.SelectedIndexes(); }
		/// <summary>
		/// Gets true if selection exists.
		/// </summary>
		public bool SelectionExists { get { return _Panel.SelectionExists; } }
		#endregion
		#region Core Events
		/// <summary>
		/// Called by <see cref="UIClosed"/>.
		/// </summary>
		public event EventHandler Closed;
		/// <summary>
		/// Called when the panel has been closed.
		/// </summary>
		/// <remarks>
		/// The method releases panel resources. It should not do anything else, the panel has gone.
		/// <para>
		/// Overriden methods must call the base. Consider to use try/finally and call the base from finally.
		/// </para>
		/// <para>
		/// The base method triggers the <see cref="Closed"/> event and then disposes the <see cref="Garbage"/>.
		/// </para>
		/// </remarks>
		public virtual void UIClosed()
		{
			try
			{
				if (Closed != null)
					Closed(this, null);
			}
			finally
			{
				if (_Garbage != null)
				{
					foreach (var it in _Garbage)
						it.Dispose();

					_Garbage = null;
				}
			}
		}
		/// <summary>
		/// Called by <see cref="UIClosing"/>.
		/// </summary>
		public event EventHandler<PanelEventArgs> Closing;
		/// <summary>
		/// Called when the panel is about to be closed.
		/// </summary>
		/// <param name="e">.</param>
		/// <remarks>
		/// There are issues:  http://bugs.farmanager.com/view.php?id=602
		/// <para>
		/// Far calls this unexpectedly on plugin commands invoked from the command line
		/// even if a new panel is not going to be opened and the current one closed.
		/// Thus, it can be called more than once.
		/// </para>
		/// </remarks>
		public virtual void UIClosing(PanelEventArgs e) //_090321_165608
		{
			if (Closing != null)
				Closing(this, e);
		}
		/// <summary>
		/// Called on invoking a command from the command line.
		/// </summary>
		/// <remarks>
		/// If the command is supported then set <see cref="PanelEventArgs.Ignore"/> = true
		/// as soon as possible before potential exceptions.
		/// </remarks>
		public event EventHandler<CommandLineEventArgs> InvokingCommand;
		///
		public bool WorksInvokingCommand(CommandLineEventArgs e)
		{
			if (InvokingCommand == null)
				return false;
			if (e != null)
				InvokingCommand(this, e);
			return true;
		}
		/// <summary>
		/// Called by <see cref="UIUpdateInfo"/>.
		/// </summary>
		public event EventHandler UpdateInfo;
		/// <summary>
		/// Called by the core to get the panel info. Use it only when it is absolutely needed.
		/// </summary>
		/// <remarks>
		/// Normally you should not use this. It is not recommended for many reasons:
		/// *) it is called frequently and can be very expensive;
		/// *) it may have problems if panel data are accessed, even for reading.
		/// <para>
		/// Consider to update the panel info in other methods.
		/// As the last resort use <see cref="UIRedrawing"/>.
		/// </para>
		/// </remarks>
		public virtual void UIUpdateInfo()
		{
			if (UpdateInfo != null)
				UpdateInfo(this, null);
		}
		/// <summary>
		/// Called by <see cref="UICtrlBreak"/>.
		/// </summary>
		public event EventHandler CtrlBreak;
		/// <summary>
		/// Called when [CtrlBreak] is pressed, normally from a separate thread.
		/// </summary>
		/// <remarks>
		/// Processing is performed in a separate thread, use only approved API and thread safe techniques.
		/// <para>
		/// The base method triggers the <see cref="CtrlBreak"/> event.
		/// </para>
		/// </remarks>
		public virtual void UICtrlBreak()
		{
			if (CtrlBreak != null)
				CtrlBreak(this, null);
		}
		/// <summary>
		/// Called by <see cref="UIRedrawing"/>.
		/// </summary>
		public event EventHandler<PanelEventArgs> Redrawing;
		/// <summary>
		/// Called when the panel is about to redraw.
		/// </summary>
		/// <param name="e">.</param>
		/// <remarks>
		/// Set <see cref="PanelEventArgs.Ignore"/> = true if the module redraws the panel itself.
		/// <para>
		/// The base method triggers the <see cref="Redrawing"/> event.
		/// </para>
		/// </remarks>
		public virtual void UIRedrawing(PanelEventArgs e)
		{
			if (Redrawing != null)
				Redrawing(this, e);
		}
		/// <summary>
		/// Called by <see cref="UIViewChanged"/>.
		/// </summary>
		public event EventHandler<ViewChangedEventArgs> ViewChanged;
		/// <summary>
		/// Called when panel view mode is changed.
		/// </summary>
		/// <param name="e">.</param>
		/// <remarks>
		/// The base method triggers the <see cref="ViewChanged"/> event.
		/// </remarks>
		public void UIViewChanged(ViewChangedEventArgs e)
		{
			if (ViewChanged != null)
				ViewChanged(this, e);
		}
		/// <summary>
		/// Called by <see cref="UIGotFocus"/>.
		/// </summary>
		public event EventHandler GotFocus;
		/// <summary>
		/// Called when the panel has got focus.
		/// </summary>
		/// <remarks>
		/// The base method triggers the <see cref="GotFocus"/> event.
		/// </remarks>
		public virtual void UIGotFocus()
		{
			if (GotFocus != null)
				GotFocus(this, null);
		}
		/// <summary>
		/// Called by <see cref="UILosingFocus"/>.
		/// </summary>
		public event EventHandler LosingFocus;
		/// <summary>
		/// Called when the panel is losing focus.
		/// </summary>
		/// <remarks>
		/// The base method triggers the <see cref="LosingFocus"/> event.
		/// </remarks>
		public virtual void UILosingFocus()
		{
			if (LosingFocus != null)
				LosingFocus(this, null);
		}
		#endregion
		#region Other Info
		/// <summary>
		/// Gets or sets the format name (shown in the file copy dialog).
		/// </summary>
		public string FormatName { get { return _Panel.FormatName; } set { _Panel.FormatName = value; } }
		/// <summary>
		/// Gets or sets the base file of emulated file system.
		/// </summary>
		/// <remarks>
		/// If the panel doesn't emulate a file system based on files it should be empty.
		/// </remarks>
		public string HostFile { get { return _Panel.HostFile; } set { _Panel.HostFile = value; } }
		#endregion
		/// <summary>
		/// Gets or sets info panel item array.
		/// </summary>
		/// <remarks>
		/// If you get it and change items then set it again after changes.
		/// Without that individual item changes will have no effect.
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		public DataItem[] InfoItems { get { return _Panel.InfoItems; } set { _Panel.InfoItems = value; } }
		/// <summary>
		/// Sets key bars.
		/// </summary>
		/// <param name="bars">The array of key bar data.</param>
		public void SetKeyBars(KeyBar[] bars) { _Panel.SetKeyBars(bars); }
		/// <summary>
		/// Gets the panel view plan or null if it is not set.
		/// </summary>
		/// <param name="mode">View mode to get the plan for.</param>
		/// <returns>
		/// The view plan. If you change it for the opened panel then call <see cref="SetPlan"/> even with the same object.
		/// </returns>
		public PanelPlan GetPlan(PanelViewMode mode) { return _Panel.GetPlan(mode); }
		/// <summary>
		/// Sets the panel plan.
		/// </summary>
		/// <param name="mode">View mode to set the plan for.</param>
		/// <param name="plan">The plan. If it has no columns then a column "Name" is assumed.</param>
		public void SetPlan(PanelViewMode mode, PanelPlan plan) { _Panel.SetPlan(mode, plan); }
		/// <summary>
		/// Selects the specified panel files.
		/// </summary>
		/// <param name="files">Collection of <see cref="FarFile"/> files to be selected.</param>
		/// <param name="comparer">The file comparer or null for the panel comparer.</param>
		/// <remarks>
		/// Call <see cref="Redraw()"/> after that.
		/// </remarks>
		public void SelectFiles(IEnumerable files, IEqualityComparer<FarFile> comparer)
		{
			// no job?
			if (files == null)
				return;

			// hash the files using the proper comparer, ignore dupes
			var hash = Works.Kit.HashFiles(files, comparer ?? Explorer.FileComparer);

			// empty
			if (hash.Count == 0)
				return;

			// indexes of files found in the hash
			int index = -1;
			var indexes = new List<int>();
			foreach (var file in ShownList)
			{
				++index;
				if (hash.ContainsKey(file))
					indexes.Add(index);
			}

			// select by indexes
			if (indexes.Count > 0)
				SelectAt(indexes.ToArray());
		}
		/// <summary>
		/// Sets the panel active.
		/// </summary>
		/// <seealso cref="IsActive"/>
		/// <exception cref="InvalidOperationException">The panel cannot be active, e.g. it is hidden.</exception>
		public void SetActive() { _Panel.SetActive(); }
	}
}
