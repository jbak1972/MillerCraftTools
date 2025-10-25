using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using Miller_Craft_Tools.UI.Styles;

namespace Miller_Craft_Tools.UI
{
    /// <summary>
    /// Showcases all UI components for visual consistency review
    /// </summary>
    public class UIShowcaseDialog : BrandedForm
    {
        /// <summary>
        /// Creates a new UI showcase dialog
        /// </summary>
        public UIShowcaseDialog()
        {
            this.Text = "Miller Craft UI Components";
            this.Size = new System.Drawing.Size(800, 600);
            
            InitializeShowcase();
            
            // Add standard footer buttons
            AddCloseButton();
            AddPrimaryButton("Apply Style", ApplyStyle_Click);
            AddStatusMessage("Status message example");
        }
        
        /// <summary>
        /// Initializes the showcase components
        /// </summary>
        private void InitializeShowcase()
        {
            // Create a tabbed interface to organize the showcase
            TabControl tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            ContentPanel.Controls.Add(tabControl);
            
            // Add tabs for different component categories
            TabPage colorsTab = new TabPage("Colors & Typography");
            TabPage buttonsTab = new TabPage("Buttons");
            TabPage statusTab = new TabPage("Status Indicators");
            TabPage iconsTab = new TabPage("Icons");
            
            tabControl.TabPages.Add(colorsTab);
            tabControl.TabPages.Add(buttonsTab);
            tabControl.TabPages.Add(statusTab);
            tabControl.TabPages.Add(iconsTab);
            
            // Colors and Typography tab
            System.Windows.Forms.Panel colorsPanel = new System.Windows.Forms.Panel();
            colorsPanel.AutoScroll = true;
            colorsPanel.Dock = DockStyle.Fill;
            colorsPanel.Padding = new System.Windows.Forms.Padding(UISettings.StandardPadding);
            colorsTab.Controls.Add(colorsPanel);
            
            // Add heading styles
            Label headingTitle = new Label();
            headingTitle.Text = "Typography";
            headingTitle.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
            headingTitle.AutoSize = true;
            UISettings.ApplyHeadingStyle(headingTitle);
            colorsPanel.Controls.Add(headingTitle);
            
            Label heading = new Label();
            heading.Text = "Heading Style";
            heading.Location = new System.Drawing.Point(UISettings.StandardPadding, headingTitle.Bottom + UISettings.StandardPadding);
            heading.AutoSize = true;
            UISettings.ApplyHeadingStyle(heading);
            colorsPanel.Controls.Add(heading);
            
            Label subheading = new Label();
            subheading.Text = "Subheading Style";
            subheading.Location = new System.Drawing.Point(UISettings.StandardPadding, heading.Bottom + UISettings.StandardPadding);
            subheading.AutoSize = true;
            UISettings.ApplySubheadingStyle(subheading);
            colorsPanel.Controls.Add(subheading);
            
            Label body = new Label();
            body.Text = "Body Style - This is the standard text style used throughout the application.";
            body.Location = new System.Drawing.Point(UISettings.StandardPadding, subheading.Bottom + UISettings.StandardPadding);
            body.AutoSize = true;
            body.MaximumSize = new System.Drawing.Size(400, 0);
            UISettings.ApplyBodyStyle(body);
            colorsPanel.Controls.Add(body);
            
            Label small = new Label();
            small.Text = "Small Style - Used for captions, help text, and other secondary information.";
            small.Location = new System.Drawing.Point(UISettings.StandardPadding, body.Bottom + UISettings.StandardPadding);
            small.AutoSize = true;
            small.MaximumSize = new System.Drawing.Size(400, 0);
            UISettings.ApplySmallStyle(small);
            colorsPanel.Controls.Add(small);
            
            // Add color swatches
            Label colorsTitle = new Label();
            colorsTitle.Text = "Brand Colors";
            colorsTitle.Location = new System.Drawing.Point(UISettings.StandardPadding, small.Bottom + UISettings.WidePadding * 2);
            colorsTitle.AutoSize = true;
            UISettings.ApplyHeadingStyle(colorsTitle);
            colorsPanel.Controls.Add(colorsTitle);
            
            int swatchSize = 60;
            int swatchSpacing = swatchSize + UISettings.StandardPadding;
            
            // Primary color
            System.Windows.Forms.Panel primarySwatch = new System.Windows.Forms.Panel();
            primarySwatch.Location = new System.Drawing.Point(UISettings.StandardPadding, colorsTitle.Bottom + UISettings.StandardPadding);
            primarySwatch.Size = new System.Drawing.Size(swatchSize, swatchSize);
            primarySwatch.BackColor = BrandColors.PrimaryColor;
            colorsPanel.Controls.Add(primarySwatch);
            
            Label primaryLabel = new Label();
            primaryLabel.Text = "Primary";
            primaryLabel.Location = new System.Drawing.Point(UISettings.StandardPadding, primarySwatch.Bottom + 5);
            primaryLabel.AutoSize = true;
            UISettings.ApplySmallStyle(primaryLabel);
            colorsPanel.Controls.Add(primaryLabel);
            
            // Secondary color
            System.Windows.Forms.Panel secondarySwatch = new System.Windows.Forms.Panel();
            secondarySwatch.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing, colorsTitle.Bottom + UISettings.StandardPadding);
            secondarySwatch.Size = new System.Drawing.Size(swatchSize, swatchSize);
            secondarySwatch.BackColor = BrandColors.SecondaryColor;
            colorsPanel.Controls.Add(secondarySwatch);
            
            Label secondaryLabel = new Label();
            secondaryLabel.Text = "Secondary";
            secondaryLabel.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing, primarySwatch.Bottom + 5);
            secondaryLabel.AutoSize = true;
            UISettings.ApplySmallStyle(secondaryLabel);
            colorsPanel.Controls.Add(secondaryLabel);
            
            // Success color
            System.Windows.Forms.Panel successSwatch = new System.Windows.Forms.Panel();
            successSwatch.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing * 2, colorsTitle.Bottom + UISettings.StandardPadding);
            successSwatch.Size = new System.Drawing.Size(swatchSize, swatchSize);
            successSwatch.BackColor = BrandColors.SuccessColor;
            colorsPanel.Controls.Add(successSwatch);
            
            Label successLabel = new Label();
            successLabel.Text = "Success";
            successLabel.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing * 2, primarySwatch.Bottom + 5);
            successLabel.AutoSize = true;
            UISettings.ApplySmallStyle(successLabel);
            colorsPanel.Controls.Add(successLabel);
            
            // Warning color
            System.Windows.Forms.Panel warningSwatch = new System.Windows.Forms.Panel();
            warningSwatch.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing * 3, colorsTitle.Bottom + UISettings.StandardPadding);
            warningSwatch.Size = new System.Drawing.Size(swatchSize, swatchSize);
            warningSwatch.BackColor = BrandColors.WarningColor;
            colorsPanel.Controls.Add(warningSwatch);
            
            Label warningLabel = new Label();
            warningLabel.Text = "Warning";
            warningLabel.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing * 3, primarySwatch.Bottom + 5);
            warningLabel.AutoSize = true;
            UISettings.ApplySmallStyle(warningLabel);
            colorsPanel.Controls.Add(warningLabel);
            
            // Error color
            System.Windows.Forms.Panel errorSwatch = new System.Windows.Forms.Panel();
            errorSwatch.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing * 4, colorsTitle.Bottom + UISettings.StandardPadding);
            errorSwatch.Size = new System.Drawing.Size(swatchSize, swatchSize);
            errorSwatch.BackColor = BrandColors.ErrorColor;
            colorsPanel.Controls.Add(errorSwatch);
            
            Label errorLabel = new Label();
            errorLabel.Text = "Error";
            errorLabel.Location = new System.Drawing.Point(UISettings.StandardPadding + swatchSpacing * 4, primarySwatch.Bottom + 5);
            errorLabel.AutoSize = true;
            UISettings.ApplySmallStyle(errorLabel);
            colorsPanel.Controls.Add(errorLabel);
            
            // Buttons tab
            System.Windows.Forms.Panel buttonsPanel = new System.Windows.Forms.Panel();
            buttonsPanel.AutoScroll = true;
            buttonsPanel.Dock = DockStyle.Fill;
            buttonsPanel.Padding = new System.Windows.Forms.Padding(UISettings.StandardPadding);
            buttonsTab.Controls.Add(buttonsPanel);
            
            Label buttonsTitle = new Label();
            buttonsTitle.Text = "Button Styles";
            buttonsTitle.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
            buttonsTitle.AutoSize = true;
            UISettings.ApplyHeadingStyle(buttonsTitle);
            buttonsPanel.Controls.Add(buttonsTitle);
            
            // Primary button
            System.Windows.Forms.Button primaryButton = new System.Windows.Forms.Button();
            primaryButton.Text = Terms.SyncButtonText;
            primaryButton.Location = new System.Drawing.Point(UISettings.StandardPadding, buttonsTitle.Bottom + UISettings.StandardPadding);
            primaryButton.Size = new System.Drawing.Size(150, 30);
            UISettings.ApplyPrimaryButtonStyle(primaryButton);
            buttonsPanel.Controls.Add(primaryButton);
            
            Label primaryButtonLabel = new Label();
            primaryButtonLabel.Text = "Primary Button";
            primaryButtonLabel.Location = new System.Drawing.Point(UISettings.StandardPadding + 160, primaryButton.Top + 5);
            primaryButtonLabel.AutoSize = true;
            UISettings.ApplyBodyStyle(primaryButtonLabel);
            buttonsPanel.Controls.Add(primaryButtonLabel);
            
            // Secondary button
            System.Windows.Forms.Button secondaryButton = new System.Windows.Forms.Button();
            secondaryButton.Text = Terms.CancelButtonText;
            secondaryButton.Location = new System.Drawing.Point(UISettings.StandardPadding, primaryButton.Bottom + UISettings.StandardPadding);
            secondaryButton.Size = new System.Drawing.Size(150, 30);
            UISettings.ApplySecondaryButtonStyle(secondaryButton);
            buttonsPanel.Controls.Add(secondaryButton);
            
            Label secondaryButtonLabel = new Label();
            secondaryButtonLabel.Text = "Secondary Button";
            secondaryButtonLabel.Location = new System.Drawing.Point(UISettings.StandardPadding + 160, secondaryButton.Top + 5);
            secondaryButtonLabel.AutoSize = true;
            UISettings.ApplyBodyStyle(secondaryButtonLabel);
            buttonsPanel.Controls.Add(secondaryButtonLabel);
            
            // Outline button
            System.Windows.Forms.Button outlineButton = new System.Windows.Forms.Button();
            outlineButton.Text = Terms.RefreshButtonText;
            outlineButton.Location = new System.Drawing.Point(UISettings.StandardPadding, secondaryButton.Bottom + UISettings.StandardPadding);
            outlineButton.Size = new System.Drawing.Size(150, 30);
            UISettings.ApplyOutlineButtonStyle(outlineButton);
            buttonsPanel.Controls.Add(outlineButton);
            
            Label outlineButtonLabel = new Label();
            outlineButtonLabel.Text = "Outline Button";
            outlineButtonLabel.Location = new System.Drawing.Point(UISettings.StandardPadding + 160, outlineButton.Top + 5);
            outlineButtonLabel.AutoSize = true;
            UISettings.ApplyBodyStyle(outlineButtonLabel);
            buttonsPanel.Controls.Add(outlineButtonLabel);
            
            // Status indicators tab
            System.Windows.Forms.Panel statusPanel = new System.Windows.Forms.Panel();
            statusPanel.AutoScroll = true;
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.Padding = new System.Windows.Forms.Padding(UISettings.StandardPadding);
            statusTab.Controls.Add(statusPanel);
            
            Label statusTitle = new Label();
            statusTitle.Text = "Status Indicators";
            statusTitle.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
            statusTitle.AutoSize = true;
            UISettings.ApplyHeadingStyle(statusTitle);
            statusPanel.Controls.Add(statusTitle);
            
            // Add status indicators for all statuses
            StatusIndicator idleStatus = new StatusIndicator();
            idleStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, statusTitle.Bottom + UISettings.StandardPadding);
            idleStatus.Status = SyncStatus.Idle;
            statusPanel.Controls.Add(idleStatus);
            
            StatusIndicator uploadingStatus = new StatusIndicator();
            uploadingStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, idleStatus.Bottom + UISettings.StandardPadding);
            uploadingStatus.Status = SyncStatus.Uploading;
            statusPanel.Controls.Add(uploadingStatus);
            
            StatusIndicator pendingStatus = new StatusIndicator();
            pendingStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, uploadingStatus.Bottom + UISettings.StandardPadding);
            pendingStatus.Status = SyncStatus.Pending;
            statusPanel.Controls.Add(pendingStatus);
            
            StatusIndicator processingStatus = new StatusIndicator();
            processingStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, pendingStatus.Bottom + UISettings.StandardPadding);
            processingStatus.Status = SyncStatus.Processing;
            statusPanel.Controls.Add(processingStatus);
            
            StatusIndicator completeStatus = new StatusIndicator();
            completeStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, processingStatus.Bottom + UISettings.StandardPadding);
            completeStatus.Status = SyncStatus.Complete;
            statusPanel.Controls.Add(completeStatus);
            
            StatusIndicator errorStatus = new StatusIndicator();
            errorStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, completeStatus.Bottom + UISettings.StandardPadding);
            errorStatus.Status = SyncStatus.Error;
            statusPanel.Controls.Add(errorStatus);
            
            // Status with additional text
            StatusIndicator additionalTextStatus = new StatusIndicator();
            additionalTextStatus.Location = new System.Drawing.Point(UISettings.StandardPadding, errorStatus.Bottom + UISettings.StandardPadding);
            additionalTextStatus.UpdateStatus(SyncStatus.Complete, "Additional status text example");
            statusPanel.Controls.Add(additionalTextStatus);
            
            // Icons tab
            System.Windows.Forms.Panel iconsPanel = new System.Windows.Forms.Panel();
            iconsPanel.AutoScroll = true;
            iconsPanel.Dock = DockStyle.Fill;
            iconsPanel.Padding = new System.Windows.Forms.Padding(UISettings.StandardPadding);
            iconsTab.Controls.Add(iconsPanel);
            
            Label iconsTitle = new Label();
            iconsTitle.Text = "Icons";
            iconsTitle.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
            iconsTitle.AutoSize = true;
            UISettings.ApplyHeadingStyle(iconsTitle);
            iconsPanel.Controls.Add(iconsTitle);
            
            // List available icons
            Label availableIconsLabel = new Label();
            availableIconsLabel.Text = "Available Icons:";
            availableIconsLabel.Location = new System.Drawing.Point(UISettings.StandardPadding, iconsTitle.Bottom + UISettings.StandardPadding);
            availableIconsLabel.AutoSize = true;
            UISettings.ApplySubheadingStyle(availableIconsLabel);
            iconsPanel.Controls.Add(availableIconsLabel);
            
            // Try to display all known icons
            string[] iconNames = new string[]
            {
                IconProvider.IconNames.Sync,
                IconProvider.IconNames.Settings,
                IconProvider.IconNames.Success,
                IconProvider.IconNames.Warning,
                IconProvider.IconNames.Error,
                IconProvider.IconNames.Info,
                IconProvider.IconNames.StatusIdle,
                IconProvider.IconNames.StatusUploading,
                IconProvider.IconNames.StatusPending,
                IconProvider.IconNames.StatusProcessing,
                IconProvider.IconNames.StatusComplete,
                IconProvider.IconNames.StatusError,
                IconProvider.IconNames.Logo,
                IconProvider.IconNames.LogoWhite
            };
            
            int iconSize = 32;
            int iconsPerRow = 5;
            int row = 0, col = 0;
            
            foreach (string iconName in iconNames)
            {
                Bitmap icon = IconProvider.GetIcon(iconName);
                
                if (icon != null)
                {
                    System.Windows.Forms.Panel iconPanel = new System.Windows.Forms.Panel();
                    iconPanel.Location = new System.Drawing.Point(
                        UISettings.StandardPadding + col * (iconSize + UISettings.WidePadding), 
                        availableIconsLabel.Bottom + UISettings.StandardPadding + row * (iconSize + UISettings.WidePadding + 20));
                    iconPanel.Size = new System.Drawing.Size(iconSize + UISettings.WidePadding, iconSize + UISettings.WidePadding + 20);
                    
                    PictureBox iconBox = new PictureBox();
                    iconBox.Location = new System.Drawing.Point(UISettings.StandardPadding, UISettings.StandardPadding);
                    iconBox.Size = new System.Drawing.Size(iconSize, iconSize);
                    iconBox.Image = icon;
                    iconBox.SizeMode = PictureBoxSizeMode.Zoom;
                    
                    Label iconNameLabel = new Label();
                    iconNameLabel.Text = iconName;
                    iconNameLabel.Location = new System.Drawing.Point(0, iconBox.Bottom + 2);
                    iconNameLabel.Size = new System.Drawing.Size(iconSize + UISettings.WidePadding, 20);
                    iconNameLabel.TextAlign = ContentAlignment.TopCenter;
                    UISettings.ApplySmallStyle(iconNameLabel);
                    
                    iconPanel.Controls.Add(iconBox);
                    iconPanel.Controls.Add(iconNameLabel);
                    iconsPanel.Controls.Add(iconPanel);
                    
                    // Move to next position
                    col++;
                    if (col >= iconsPerRow)
                    {
                        col = 0;
                        row++;
                    }
                }
            }
        }
        
        /// <summary>
        /// Event handler for the Apply Style button
        /// </summary>
        private void ApplyStyle_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "This is a message box with consistent styling applied.",
                Terms.SuccessText,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
