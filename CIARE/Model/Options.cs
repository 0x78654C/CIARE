using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.FilesOpenOS;
using CIARE.Utils.OpenAISettings;
using CIARE.Utils.Options;
using ICSharpCode.TextEditor;
using OllamaInt;
using CodexInt;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public partial class Options : Form
    {
        int _tokenTxtLen = 0;
        public static Options Instance { get; private set; }
        public Options()
        {
            InitializeComponent();
        }


        /// <summary>
        /// Closes the current form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Options_Load(object sender, EventArgs e)
        {
            Instance = this;
            InitializeEditor.ReadEditorHighlight(GlobalVariables.registryPath, SelectedEditor.GetSelectedEditor(), highlightCMB);
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            codeCompletionCkb.Checked = GlobalVariables.OCodeCompletion;
            lineNumberCkb.Checked = GlobalVariables.OLineNumber;
            codeFoldingCkb.Checked = GlobalVariables.OFoldingCode;
            startBehaveCkb.Checked = GlobalVariables.OStartUp;
            winLoginCkb.Checked = GlobalVariables.OWinLoginState;
            unsafeCkb.Checked = GlobalVariables.OUnsafeCode;
            publishCkb.Checked = GlobalVariables.OPublishNative;
            apiUrlTxt.Text = GlobalVariables.apiUrl;
            CheckOllama();
            var isNullAPIKey = false;
            if (GlobalVariables.aiTypeVar.StartsWith("Ollama"))
            {
                AiTypeCombo.Text = GlobalVariables.aiTypeVar;
                isNullAPIKey = string.IsNullOrEmpty(GetStoredApiKey());
                if (isNullAPIKey)
                    WaterMark.TextBoxWaterMark(apiKeyAiTxtBox, GetApiKeyWatermark(GlobalVariables.aiTypeVar));
                else
                    apiKeyAiTxtBox.Text = "******************************************";
                maxTokensTxtBox.Text = GlobalVariables.aiMaxTokens;
                modelLocalCombo.Text = GlobalVariables.modelOllamaVar;
                openAISaveBtn.Enabled = true;
                FrmColorMod.SetButtonColorDisableCombo(openAISaveBtn, modelLocalCombo, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            }
            else
                FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            AiTypeCombo.Text = GlobalVariables.aiTypeVar;
            isNullAPIKey = string.IsNullOrEmpty(GetStoredApiKey());
            if(isNullAPIKey)
                WaterMark.TextBoxWaterMark(apiKeyAiTxtBox, GetApiKeyWatermark(GlobalVariables.aiTypeVar));
            else
                apiKeyAiTxtBox.Text = "******************************************";
            maxTokensTxtBox.Text = GlobalVariables.aiMaxTokens;
            modelTxt.Text = GlobalVariables.model;
            UpdateAIActionButtons();
            TargetFramework.GetFramework(frameWorkCMB, GlobalVariables.registryPath);
            BuildConfig.SetConfigControl(configurationBox);
            BuildConfig.SetPlatformControl(platformBox);
            _tokenTxtLen = maxTokensTxtBox.Text.Length;
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        /// <summary>
        /// Hightlight set for text editor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void highlightCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabs = MainForm.Instance.EditorTabControl;
            int count = 0;
            foreach (TabPage tab in tabs.TabPages)
            {
                if (count > 0)
                {
                    Control ctrl = MainForm.Instance.EditorTabControl.Controls[count].Controls[0];
                    var textEditor = ctrl as TextEditorControl;
                    MainForm.Instance.SetHighLighter(textEditor, highlightCMB.Text);
                }
                count++;
            }
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        private void codeCompletionCkb_CheckedChanged(object sender, EventArgs e)
        {
            CodeCompletion.SetCodeCompletionStatus(codeCompletionCkb, GlobalVariables.codeCompletionKey);
        }

        private void lineNumberCkb_CheckedChanged(object sender, EventArgs e)
        {
            LineNumber.SetLineNumberStatus(lineNumberCkb, GlobalVariables.lineNumberKey);
        }

        private void codeFoldingCkb_CheckedChanged(object sender, EventArgs e)
        {
            FoldingCode.SetFoldingCodeStatus(codeFoldingCkb, GlobalVariables.foldingCodeKey);
        }

        private void configurationBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (configurationBox.Text == "Release")
                GlobalVariables.configParam = "/p:configuration=Release";
            else
                GlobalVariables.configParam = "/p:configuration=Debug";
            BuildConfig.StoreConfig(GlobalVariables.OConfigParam, GlobalVariables.configParam);
        }

        private void platformBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string platform = platformBox.Text;
            if(platform == "AnyCPU")
                platform = "Any CPU";
            GlobalVariables.platformParam = $"/p:Platform=\"{platform}\"";
            BuildConfig.StorePlatform(GlobalVariables.OPlatformParam, GlobalVariables.platformParam);
        }

        private void startBehaveCkb_CheckedChanged(object sender, EventArgs e)
        {
            StartFilesOS.SetOSStartStatus(startBehaveCkb, GlobalVariables.startUp);
        }

        /// <summary>
        /// Check and set state of Windows login checkbox.
        /// </summary>
        /// <param name="markFileCkb"></param>
        /// <param name="winLoginCkb"></param>
        private void CheckMarkFileActivation(CheckBox markFileCkb, CheckBox winLoginCkb)
        {
            if (markFileCkb.Checked)
                winLoginCkb.Enabled = true;
            else
            {
                winLoginCkb.Enabled = false;
                winLoginCkb.Checked = false;
            }
        }

        private void winLoginCkb_CheckedChanged(object sender, EventArgs e)
        {
            var autoStartFile = new AutoStartFile(GlobalVariables.regUserRunPath, GlobalVariables.markFile, GlobalVariables.markFile, GlobalVariables.openedFilePath);
            autoStartFile.SetRegistryRunApp(winLoginCkb);
            StartFilesOS.SetWinLoginState(winLoginCkb, GlobalVariables.OWinLogin);
        }

        private void frameWorkCMB_SelectedIndexChanged(object sender, EventArgs e)
        {
            TargetFramework.SetFramework(frameWorkCMB, GlobalVariables.OFramework);
        }

        /// <summary>
        /// Store API url for live share.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveApiUrlBtn_Click(object sender, EventArgs e)
        {
            LiveShare.SetApiLiveShare(apiUrlTxt, GlobalVariables.liveShare);
            MessageBox.Show("API url was saved!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Enable save button if apiUrlTxt is not empty.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void apiUrlTxt_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(apiUrlTxt.Text))
                saveApiUrlBtn.Enabled = true;
            else
                saveApiUrlBtn.Enabled = false;
            FrmColorMod.SetButtonColorDisable(saveApiUrlBtn, apiUrlTxt, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        /// <summary>
        /// Store OpenAI API key and max tokens.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openAISaveBtn_Click(object sender, EventArgs e)
        {
            OpenAISetting.SetOpenAIData(apiKeyAiTxtBox, maxTokensTxtBox, modelTxt, AiTypeCombo, modelLocalCombo, GlobalVariables.openAIKey, GlobalVariables.openAIMaxTokens, GlobalVariables.openModel, GlobalVariables.ollamModel, GlobalVariables.aiType);
            MessageBox.Show("AI settings are saved!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Enable save button if apiKeyAiTxtBox is not empty.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void apiKeyAiTxtBox_TextChanged(object sender, EventArgs e)
        {
            if (AiTypeCombo.Text == "GitHub Copilot" ||
                AiTypeCombo.Text == "OpenAI Codex" ||
                AiTypeCombo.Text.StartsWith("Ollama"))
                openAISaveBtn.Enabled = true;
            else if (!string.IsNullOrEmpty(apiKeyAiTxtBox.Text))
                openAISaveBtn.Enabled = true;
            else
                openAISaveBtn.Enabled = false;
            FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
        }

        /// <summary>
        /// Check if field contains numbers only on text changed!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void maxTokensTxtBox_TextChanged(object sender, EventArgs e)
        {
            if (!DataManage.IsNumberAllowed(maxTokensTxtBox.Text))
            {
                MessageBox.Show("Field must contain numbers only!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                maxTokensTxtBox.Text = maxTokensTxtBox.Text.Substring(0, _tokenTxtLen);
                maxTokensTxtBox.SelectionStart = maxTokensTxtBox.Text.Length;
                maxTokensTxtBox.ScrollToCaret();
                return;
            }
        }

        /// <summary>
        /// Enable unsafe code use.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void unsafeCkb_CheckedChanged(object sender, EventArgs e)
        {
            UnsafeCode.SetUnsafeStatus(unsafeCkb, GlobalVariables.unsafeCode);
        }

        /// <summary>
        /// Enalble publish
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void publishCkb_CheckedChanged(object sender, EventArgs e)
        {
            Publish.SetPublishStatus(publishCkb, GlobalVariables.publish);
        }

        /// <summary>
        /// AI selection logic.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AiTypeCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var isOllama = AiTypeCombo.Text.StartsWith("Ollama");
            if (isOllama)
            {
                apiKeyAiTxtBox.Enabled = !isOllama;
                maxTokensTxtBox.Enabled = !isOllama;
                modelTxt.Enabled = !isOllama;
                if (isOllama)
                    AiManage.LoadOllamaModels(ref modelLocalCombo);
                modelLocalCombo.Visible = true;
                modelLocalLbl.Visible = true;
                openAISaveBtn.Enabled = true;
                FrmColorMod.SetButtonColorDisableCombo(openAISaveBtn, modelLocalCombo, GlobalVariables.darkColor, GlobalVariables.isVStheme);
                UpdateAIActionButtons();
            }
            else
            {
                apiKeyAiTxtBox.Enabled = AiTypeCombo.Text != "GitHub Copilot";
                maxTokensTxtBox.Enabled = true;
                modelTxt.Enabled = true;
                modelLocalCombo.Visible = false;
                modelLocalLbl.Visible = false;
                UpdateAIActionButtons();
                WaterMark.TextBoxWaterMark(apiKeyAiTxtBox, GetApiKeyWatermark(AiTypeCombo.Text));
                if (string.IsNullOrEmpty(modelTxt.Text) || IsKnownDefaultModel(modelTxt.Text))
                    modelTxt.Text = GetDefaultModel(AiTypeCombo.Text);
                if (AiTypeCombo.Text == "GitHub Copilot" || AiTypeCombo.Text == "OpenAI Codex")
                    openAISaveBtn.Enabled = true;
                else if (string.IsNullOrEmpty(apiKeyAiTxtBox.Text))
                    openAISaveBtn.Enabled = false;
                else
                    openAISaveBtn.Enabled = true;
                FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
            }
        }

        private static string GetApiKeyWatermark(string aiType)
        {
            return aiType switch
            {
                "GitHub Copilot" => "Enter GitHub PAT (needs 'copilot' scope)......",
                "OpenAI Codex" => "Uses Codex CLI login; API key optional.....",
                "OpenRouter" => "Enter OpenRouter API key...................",
                _ => "Enter OpenAI API key.......................",
            };
        }

        private static string GetDefaultModel(string aiType)
        {
            return aiType switch
            {
                "GitHub Copilot" => "claude-3.7-sonnet",
                "OpenAI Codex" => "gpt-5.3-codex",
                "OpenRouter" => "openai/gpt-3.5-turbo",
                _ => "text-davinci-003",
            };
        }

        private static bool IsKnownDefaultModel(string model)
        {
            return model == "text-davinci-003" ||
                   model == "openai/gpt-3.5-turbo" ||
                   model == "claude-3.7-sonnet" ||
                   model == "gpt-5.3-codex";
        }

        private static bool HasProviderActions(string aiType)
        {
            return aiType == "GitHub Copilot" || aiType == "OpenAI Codex";
        }

        private void UpdateAIActionButtons()
        {
            var hasActions = HasProviderActions(AiTypeCombo.Text);
            listModelsBtn.Visible = hasActions;
            connectCopilotBtn.Visible = hasActions;
            connectCopilotBtn.Text = AiTypeCombo.Text == "OpenAI Codex" ? "Connect Codex" : "Sign in to GitHub";
        }

        private string GetApiKeyFromForm()
        {
            var typedKey = apiKeyAiTxtBox.Text.Trim();
            if (typedKey.StartsWith("***"))
                return GetStoredApiKey();

            return typedKey;
        }

        private static string GetStoredApiKey()
        {
            return GlobalVariables.aiKey == null
                ? string.Empty
                : GlobalVariables.aiKey.ConvertSecureStringToString();
        }

        private string GetExplicitCodexApiKeyFromForm()
        {
            var typedKey = apiKeyAiTxtBox.Text.Trim();
            return typedKey.StartsWith("***") ? string.Empty : typedKey;
        }

        /// <summary>
        /// List available models for the connected AI service. For GitHub Copilot, this uses the official API which requires no additional authentication beyond the stored OAuth token. For other AI types, this may not be supported or may require a separate API key. The results are displayed in a message box. Errors are caught and shown to the user as well.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void listModelsBtn_Click(object sender, EventArgs e)
        {
            listModelsBtn.Enabled = false;
            listModelsBtn.Text = "Loading...";
            try
            {
                if (AiTypeCombo.Text == "OpenAI Codex")
                {
                    var codexClient = await CreateCodexClientFromFormAsync();
                    var selection = ShowModelSelectionDialog(
                        "OpenAI Codex - Select Model",
                        await codexClient.GetModelsAsync());
                    if (!string.IsNullOrWhiteSpace(selection.ModelId))
                    {
                        modelTxt.Text = selection.ModelId;
                        GlobalVariables.model = selection.ModelId;
                        GlobalVariables.codexReasoningLevelVar = selection.ReasoningLevel;
                        RegistryManagement.RegKey_WriteSubkey(
                            GlobalVariables.registryPath,
                            GlobalVariables.codexReasoningLevel,
                            selection.ReasoningLevel);
                        openAISaveBtn.Enabled = true;
                        FrmColorMod.SetButtonColorDisable(openAISaveBtn, apiKeyAiTxtBox, GlobalVariables.darkColor, GlobalVariables.isVStheme);
                    }
                    return;
                }

                // ListModelsAsync authenticates via the stored Copilot OAuth token (no PAT required).
                // The PAT is only needed as a fallback for GitHub Models API.
                var copilotToken = GlobalVariables.copilotOAuthToken?.ConvertSecureStringToString() ?? string.Empty;
                var pat = GetStoredApiKey();
                if (string.IsNullOrEmpty(pat))
                    pat = apiKeyAiTxtBox.Text.Trim();
                if (pat.StartsWith("***"))
                    pat = string.Empty;
                var copilotClient = new CopilotInt.CopilotClient(copilotToken, pat);
                var copilotModels = await copilotClient.ListModelsAsync();
                MessageBox.Show(copilotModels, "GitHub Copilot — Available Models", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                var title = AiTypeCombo.Text == "OpenAI Codex" ? "OpenAI Codex" : "GitHub Copilot";
                MessageBox.Show($"Error: {ex.Message}", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listModelsBtn.Enabled = true;
                listModelsBtn.Text = "List Models";
                UpdateAIActionButtons();
            }
        }

        private ModelSelection ShowModelSelectionDialog(string title, IEnumerable<CodexModelInfo> models)
        {
            var items = models
                .Select(model => new ModelListItem(model))
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .ToList();

            if (items.Count == 0)
            {
                MessageBox.Show("No models returned.", title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return ModelSelection.Empty;
            }

            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(620, 470),
                MinimumSize = new Size(520, 360),
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ShowInTaskbar = false,
                Font = Font
            };

            var filterTxt = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 8, 8, 4)
            };

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                HorizontalScrollbar = true,
                Margin = new Padding(8, 0, 8, 8)
            };

            var reasoningLbl = new Label
            {
                AutoSize = true,
                Text = "Reasoning Level:",
                Margin = new Padding(8, 7, 8, 0)
            };

            var reasoningCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 260,
                Margin = new Padding(0, 4, 8, 0)
            };

            var reasoningPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 2, 0, 0)
            };
            reasoningPanel.Controls.Add(reasoningLbl);
            reasoningPanel.Controls.Add(reasoningCombo);

            var okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 92,
                Height = 28
            };

            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 92,
                Height = 28
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.Controls.Add(filterTxt, 0, 0);
            layout.Controls.Add(listBox, 0, 1);
            layout.Controls.Add(reasoningPanel, 0, 2);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 44,
                Padding = new Padding(8)
            };
            buttons.Controls.Add(okButton);
            buttons.Controls.Add(cancelButton);

            form.Controls.Add(layout);
            form.Controls.Add(buttons);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            void LoadItems()
            {
                var selectedId = (listBox.SelectedItem as ModelListItem)?.Id;
                if (string.IsNullOrWhiteSpace(selectedId))
                    selectedId = modelTxt.Text.Trim();

                var filter = filterTxt.Text.Trim();
                listBox.BeginUpdate();
                listBox.Items.Clear();
                foreach (var item in items)
                {
                    if (item.Matches(filter))
                        listBox.Items.Add(item);
                }

                SelectModelItem(listBox, selectedId);
                if (listBox.SelectedIndex < 0 && listBox.Items.Count > 0)
                    listBox.SelectedIndex = 0;
                okButton.Enabled = listBox.SelectedItem != null;
                LoadReasoningLevels(reasoningCombo, listBox.SelectedItem as ModelListItem, GlobalVariables.codexReasoningLevelVar);
                listBox.EndUpdate();
            }

            filterTxt.TextChanged += (_, _) => LoadItems();
            listBox.SelectedIndexChanged += (_, _) =>
            {
                okButton.Enabled = listBox.SelectedItem != null;
                LoadReasoningLevels(reasoningCombo, listBox.SelectedItem as ModelListItem, GlobalVariables.codexReasoningLevelVar);
            };
            listBox.DoubleClick += (_, _) =>
            {
                if (listBox.SelectedItem == null)
                    return;

                form.DialogResult = DialogResult.OK;
                form.Close();
            };

            FrmColorMod.ToogleColorMode(form, GlobalVariables.darkColor);
            WaterMark.TextBoxWaterMark(filterTxt, "Filter models...");
            LoadItems();

            return form.ShowDialog(this) == DialogResult.OK && listBox.SelectedItem is ModelListItem selected
                ? new ModelSelection(selected.Id, GetSelectedReasoningLevel(reasoningCombo))
                : ModelSelection.Empty;
        }

        private static void LoadReasoningLevels(ComboBox comboBox, ModelListItem item, string preferredReasoningLevel)
        {
            var current = (comboBox.SelectedItem as ReasoningLevelItem)?.Effort;
            var preferred = string.IsNullOrWhiteSpace(current)
                ? preferredReasoningLevel
                : current;
            var levels = item == null ? GetFallbackReasoningLevels() : item.GetReasoningLevelItems();
            if (levels.Count == 0)
                levels = GetFallbackReasoningLevels();

            comboBox.BeginUpdate();
            comboBox.Items.Clear();
            foreach (var level in levels)
                comboBox.Items.Add(level);

            SelectReasoningLevel(comboBox, preferred);
            if (comboBox.SelectedIndex < 0 && item != null)
                SelectReasoningLevel(comboBox, item.DefaultReasoningLevel);
            if (comboBox.SelectedIndex < 0)
                SelectReasoningLevel(comboBox, "medium");
            if (comboBox.SelectedIndex < 0 && comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
            comboBox.EndUpdate();
        }

        private static void SelectReasoningLevel(ComboBox comboBox, string reasoningLevel)
        {
            if (string.IsNullOrWhiteSpace(reasoningLevel))
                return;

            for (var i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is ReasoningLevelItem item &&
                    string.Equals(item.Effort, reasoningLevel, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private static string GetSelectedReasoningLevel(ComboBox comboBox)
        {
            return comboBox.SelectedItem is ReasoningLevelItem item &&
                   !string.IsNullOrWhiteSpace(item.Effort)
                ? item.Effort
                : "medium";
        }

        private static List<ReasoningLevelItem> GetFallbackReasoningLevels()
        {
            return new List<ReasoningLevelItem>
            {
                new("low", "Fast responses with lighter reasoning"),
                new("medium", "Balanced reasoning"),
                new("high", "Greater reasoning depth"),
                new("xhigh", "Extra high reasoning depth")
            };
        }

        private static void SelectModelItem(ListBox listBox, string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return;

            for (var i = 0; i < listBox.Items.Count; i++)
            {
                if (listBox.Items[i] is ModelListItem item &&
                    string.Equals(item.Id, modelId, StringComparison.OrdinalIgnoreCase))
                {
                    listBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private sealed class ModelListItem
        {
            public string Id { get; }
            public string DefaultReasoningLevel { get; }
            private string DisplayName { get; }
            private string OwnedBy { get; }
            private List<CodexReasoningLevel> ReasoningLevels { get; }

            public ModelListItem(CodexModelInfo model)
            {
                Id = model.ModelId?.Trim() ?? string.Empty;
                DefaultReasoningLevel = model.default_reasoning_level?.Trim() ?? string.Empty;
                DisplayName = model.DisplayName?.Trim() ?? string.Empty;
                OwnedBy = model.owned_by?.Trim() ?? string.Empty;
                ReasoningLevels = model.supported_reasoning_levels ?? new List<CodexReasoningLevel>();
            }

            public bool Matches(string filter)
            {
                return string.IsNullOrWhiteSpace(filter) ||
                       Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                       DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                       OwnedBy.Contains(filter, StringComparison.OrdinalIgnoreCase);
            }

            public override string ToString()
            {
                var details = new List<string>();
                if (!string.IsNullOrWhiteSpace(DisplayName) &&
                    !string.Equals(DisplayName, Id, StringComparison.OrdinalIgnoreCase))
                    details.Add(DisplayName);
                if (!string.IsNullOrWhiteSpace(OwnedBy))
                    details.Add(OwnedBy);

                return details.Count == 0
                    ? Id
                    : $"{Id} ({string.Join(", ", details)})";
            }

            public List<ReasoningLevelItem> GetReasoningLevelItems()
            {
                return ReasoningLevels
                    .Where(level => !string.IsNullOrWhiteSpace(level.effort))
                    .Select(level => new ReasoningLevelItem(level.effort.Trim(), level.description?.Trim() ?? string.Empty))
                    .ToList();
            }
        }

        private sealed class ReasoningLevelItem
        {
            public string Effort { get; }
            private string Description { get; }

            public ReasoningLevelItem(string effort, string description)
            {
                Effort = effort;
                Description = description;
            }

            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(Description)
                    ? Effort
                    : $"{Effort} - {Description}";
            }
        }

        private sealed class ModelSelection
        {
            public static ModelSelection Empty { get; } = new(string.Empty, "medium");

            public string ModelId { get; }
            public string ReasoningLevel { get; }

            public ModelSelection(string modelId, string reasoningLevel)
            {
                ModelId = modelId;
                ReasoningLevel = string.IsNullOrWhiteSpace(reasoningLevel)
                    ? "medium"
                    : reasoningLevel;
            }
        }

        /// <summary>
        /// Connect/sign in to GitHub Copilot using the device authorization flow. This requires user interaction to open a browser, navigate to the verification URL, and enter the provided user code. The method handles the entire flow, including polling for the OAuth token and storing it securely once obtained. Errors and timeouts are handled gracefully with user feedback via message boxes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void connectCopilotBtn_Click(object sender, EventArgs e)
        {
            connectCopilotBtn.Enabled = false;
            connectCopilotBtn.Text = "Connecting...";
            try
            {
                if (AiTypeCombo.Text == "OpenAI Codex")
                {
                    await ConnectCodexAsync();
                    return;
                }

                var (userCode, verificationUri, deviceCode, intervalSec, error) =
                    await CopilotInt.CopilotClient.StartDeviceAuthAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show($"Could not start sign-in: {error}", "GitHub Copilot", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Copy code to clipboard and open browser before showing the dialog.
                Clipboard.SetText(userCode);
                Process.Start(new ProcessStartInfo(verificationUri) { UseShellExecute = true });

                // Start polling in the background (runs while the dialog is shown).
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(10));
                var pollTask = CopilotInt.CopilotClient.PollForTokenAsync(deviceCode, intervalSec, cts.Token);

                MessageBox.Show(
                    $"A browser tab has opened. If prompted, enter this code:\n\n   {userCode}\n\n" +
                    "(Code already copied to clipboard.)\n\nClick OK after you have authorized in the browser.",
                    "GitHub Copilot — Sign In", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Wait up to 15 more seconds after the dialog is dismissed.
                if (!pollTask.IsCompleted)
                    await Task.WhenAny(pollTask, Task.Delay(15_000));

                cts.Cancel();

                if (!pollTask.IsCompleted)
                {
                    MessageBox.Show("Authorization timed out. Please try again.", "GitHub Copilot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var (oauthToken, tokenError) = await pollTask;
                if (string.IsNullOrEmpty(oauthToken))
                {
                    MessageBox.Show($"Sign-in failed: {tokenError}", "GitHub Copilot", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Persist the token (DPAPI-encrypted in registry) and cache it in memory.
                var encrypted = CIARE.Utils.Encryption.DPAPI.Encrypt(oauthToken);
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, GlobalVariables.copilotTokenKey, encrypted);
                GlobalVariables.copilotOAuthToken = oauthToken.StringToSecureString();

                MessageBox.Show("Successfully signed in to GitHub Copilot!", "GitHub Copilot", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                var title = AiTypeCombo.Text == "OpenAI Codex" ? "OpenAI Codex" : "GitHub Copilot";
                MessageBox.Show($"Error: {ex.Message}", title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                connectCopilotBtn.Enabled = true;
                UpdateAIActionButtons();
            }
        }

        private async Task ConnectCodexAsync()
        {
            var apiKey = GetExplicitCodexApiKeyFromForm();
            CodexClient client;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                var credentials = await CodexCliAuth.TryLoadAsync();
                if (credentials == null)
                {
                    if (StartCodexLogin())
                    {
                        MessageBox.Show(
                            "A Codex CLI login window was opened.\n\nFinish signing in with ChatGPT, then click OK here.",
                            "OpenAI Codex", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        credentials = await CodexCliAuth.TryLoadAsync();
                    }
                }

                if (credentials == null)
                {
                    MessageBox.Show(
                        "Codex CLI sign-in was not found. Run `codex login` from a terminal, or enter an OpenAI API key.",
                        "OpenAI Codex", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                client = new CodexClient(credentials);
            }
            else
            {
                client = new CodexClient(apiKey);
            }

            var (ok, message) = await client.SignInAsync();
            if (!ok)
            {
                MessageBox.Show(message, "OpenAI Codex", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            OpenAISetting.SetOpenAIData(apiKeyAiTxtBox, maxTokensTxtBox, modelTxt, AiTypeCombo, modelLocalCombo, GlobalVariables.openAIKey, GlobalVariables.openAIMaxTokens, GlobalVariables.openModel, GlobalVariables.ollamModel, GlobalVariables.aiType);
            MessageBox.Show(message, "OpenAI Codex", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task<CodexClient> CreateCodexClientFromFormAsync()
        {
            var apiKey = GetExplicitCodexApiKeyFromForm();
            if (!string.IsNullOrWhiteSpace(apiKey))
                return new CodexClient(apiKey);

            var credentials = await CodexCliAuth.TryLoadAsync();
            if (credentials == null)
                throw new InvalidOperationException("Codex CLI sign-in was not found. Run `codex login`, or enter an OpenAI API key.");

            return new CodexClient(credentials);
        }

        private static bool StartCodexLogin()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c start \"Codex Login\" cmd /k codex login",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not start Codex CLI login: {ex.Message}\n\nRun `codex login` from a terminal, then try Connect Codex again.",
                    "OpenAI Codex", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Load ollama in AI combo if installed
        /// </summary>
        private void CheckOllama()
        {
            var client = new OllamaLLM();
            var isOllama = client.IsOllamaInstalled();
            if (!isOllama)
                return;
            AiTypeCombo.Items.Add("Ollama(Local)");
        }
    }
}
