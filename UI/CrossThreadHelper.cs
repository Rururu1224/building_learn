using System;
using System.Windows.Forms;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 跨线程UI更新工具类
    /// 封装 Invoke 处理后台仿真事件，解决跨线程操作UI控件崩溃问题
    /// </summary>
    public static class CrossThreadHelper
    {
        /// <summary>
        /// 安全地在UI线程执行操作
        /// 如果当前已在UI线程则直接执行，否则通过Invoke调度
        /// </summary>
        /// <param name="control">任意UI控件（用于判断InvokeRequired）</param>
        /// <param name="action">要执行的UI操作</param>
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control == null || control.IsDisposed)
                return;

            if (control.InvokeRequired)
            {
                try
                {
                    control.Invoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                }
                catch (InvalidOperationException)
                {
                    // 窗口句柄未创建等异常，忽略
                }
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// 安全地在UI线程执行操作（异步，不阻塞调用线程）
        /// </summary>
        /// <param name="control">任意UI控件</param>
        /// <param name="action">要执行的UI操作</param>
        public static void SafeBeginInvoke(this Control control, Action action)
        {
            if (control == null || control.IsDisposed)
                return;

            if (control.InvokeRequired)
            {
                try
                {
                    control.BeginInvoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放，忽略
                }
                catch (InvalidOperationException)
                {
                    // 窗口句柄未创建等异常，忽略
                }
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// 安全更新Label文本
        /// </summary>
        public static void SetTextSafe(this Label label, string text)
        {
            label.SafeInvoke(() => label.Text = text);
        }

        /// <summary>
        /// 安全更新TextBox文本
        /// </summary>
        public static void SetTextSafe(this TextBox textBox, string text)
        {
            textBox.SafeInvoke(() => textBox.Text = text);
        }

        /// <summary>
        /// 安全更新RichTextBox，追加带颜色的文本
        /// </summary>
        public static void AppendColoredTextSafe(this RichTextBox rtb, string text,
            System.Drawing.Color color)
        {
            rtb.SafeInvoke(() =>
            {
                rtb.SelectionStart = rtb.TextLength;
                rtb.SelectionLength = 0;
                rtb.SelectionColor = color;
                rtb.AppendText(text);
                rtb.SelectionColor = rtb.ForeColor;
                rtb.ScrollToCaret();
            });
        }

        /// <summary>
        /// 安全设置控件启用/禁用状态
        /// </summary>
        public static void SetEnabledSafe(this Control control, bool enabled)
        {
            control.SafeInvoke(() => control.Enabled = enabled);
        }

        /// <summary>
        /// 安全设置控件可见性
        /// </summary>
        public static void SetVisibleSafe(this Control control, bool visible)
        {
            control.SafeInvoke(() => control.Visible = visible);
        }
    }
}