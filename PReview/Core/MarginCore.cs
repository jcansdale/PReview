﻿using System;
using System.Windows;
using System.Windows.Media;
using PReview.Git;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace PReview.Core
{
    internal sealed class MarginCore : IMarginCore, IDisposable
    {
        private readonly IWpfTextView _textView;

        private readonly IClassificationFormatMap _classificationFormatMap;
        private readonly IEditorFormatMap _editorFormatMap;


        private Brush _additionBrush;
        private Brush _modificationBrush;
        private Brush _removedBrush;

        private bool _isDisposed;

        public MarginCore(IWpfTextView textView, IClassificationFormatMapService classificationFormatMapService, IEditorFormatMapService editorFormatMapService)
        {
            _textView = textView;

            _classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(textView);

            _editorFormatMap = editorFormatMapService.GetEditorFormatMap(textView);
            _editorFormatMap.FormatMappingChanged += HandleFormatMappingChanged;

            _textView.Options.OptionChanged += HandleOptionChanged;

            _textView.Closed += (sender, e) =>
            {
                _editorFormatMap.FormatMappingChanged -= HandleFormatMappingChanged;
            };

            UpdateBrushes();
        }

        public FontFamily FontFamily
        {
            get
            {
                if (_classificationFormatMap.DefaultTextProperties.TypefaceEmpty)
                    return new FontFamily("Consolas");

                return _classificationFormatMap.DefaultTextProperties.Typeface.FontFamily;
            }
        }

        public FontStretch FontStretch
        {
            get
            {
                if (_classificationFormatMap.DefaultTextProperties.TypefaceEmpty)
                    return FontStretches.Normal;

                return _classificationFormatMap.DefaultTextProperties.Typeface.Stretch;
            }
        }

        public FontStyle FontStyle
        {
            get
            {
                if (_classificationFormatMap.DefaultTextProperties.TypefaceEmpty)
                    return FontStyles.Normal;

                return _classificationFormatMap.DefaultTextProperties.Typeface.Style;
            }
        }

        public FontWeight FontWeight
        {
            get
            {
                if (_classificationFormatMap.DefaultTextProperties.TypefaceEmpty)
                    return FontWeights.Normal;

                return _classificationFormatMap.DefaultTextProperties.Typeface.Weight;
            }
        }

        public double FontSize
        {
            get
            {
                if (_classificationFormatMap.DefaultTextProperties.FontRenderingEmSizeEmpty)
                    return 12.0;

                return _classificationFormatMap.DefaultTextProperties.FontRenderingEmSize;
            }
        }

        public Brush Background
        {
            get
            {
                if (_classificationFormatMap.DefaultTextProperties.BackgroundBrushEmpty)
                    return _textView.Background;

                return _classificationFormatMap.DefaultTextProperties.BackgroundBrush;
            }
        }

        public Brush Foreground
        {
            get
            {
                if (_classificationFormatMap.DefaultTextProperties.ForegroundBrushEmpty)
                    return (Brush)Application.Current.Resources[VsBrushes.ToolWindowTextKey];

                return _classificationFormatMap.DefaultTextProperties.ForegroundBrush;
            }
        }

        public Brush AdditionBrush
        {

            get
            {
                return _additionBrush ?? Brushes.Transparent;
            }
        }

        public Brush ModificationBrush
        {
            get
            {
                return _modificationBrush ?? Brushes.Transparent;
            }
        }

        public Brush RemovedBrush
        {
            get
            {
                return _removedBrush ?? Brushes.Transparent;
            }
        }

        public double EditorChangeLeft
        {
            get { return 2.5; }
        }

        public double EditorChangeWidth
        {
            get { return 5.0; }
        }

        public double ScrollChangeWidth
        {
            get { return 3.0; }
        }

        public event EventHandler BrushesChanged;

        public void MoveToChange(int lineNumber)
        {
            var diffLine = _textView.TextSnapshot.GetLineFromLineNumber(lineNumber);

            _textView.VisualElement.Focus();
            _textView.Caret.MoveTo(diffLine.Start);
            _textView.Caret.EnsureVisible();
        }

        private void HandleFormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            if (e.ChangedItems.Contains(DiffFormatNames.Addition)
                || e.ChangedItems.Contains(DiffFormatNames.Modification)
                || e.ChangedItems.Contains(DiffFormatNames.Removed))
            {
                UpdateBrushes();
            }
        }

        private void UpdateBrushes()
        {
            _additionBrush = GetBrush(_editorFormatMap.GetProperties(DiffFormatNames.Addition));
            _modificationBrush = GetBrush(_editorFormatMap.GetProperties(DiffFormatNames.Modification));
            _removedBrush = GetBrush(_editorFormatMap.GetProperties(DiffFormatNames.Removed));
            OnBrushesChanged(EventArgs.Empty);
        }

        private void OnBrushesChanged(EventArgs e)
        {
            var t = BrushesChanged;
            if (t != null)
                t(this, e);
        }

        private static Brush GetBrush(ResourceDictionary properties)
        {
            if (properties == null)
                return Brushes.Transparent;

            if (properties.Contains(EditorFormatDefinition.BackgroundColorId))
            {
                var color = (Color)properties[EditorFormatDefinition.BackgroundColorId];
                var brush = new SolidColorBrush(color);
                if (brush.CanFreeze)
                {
                    brush.Freeze();
                }
                return brush;
            }
            if (properties.Contains(EditorFormatDefinition.BackgroundBrushId))
            {
                var brush = (Brush)properties[EditorFormatDefinition.BackgroundBrushId];
                if (brush.CanFreeze)
                {
                    brush.Freeze();
                }
                return brush;
            }

            return Brushes.Transparent;
        }

        private void HandleOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if (_isDisposed)
                return;
        }

        public ITextDocument GetTextDocument()
        {
            ITextDocument document;
            _textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out document);
            return document;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
        }
    }
}