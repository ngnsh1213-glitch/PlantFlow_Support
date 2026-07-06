using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PlantFlow_Support
{
    // 페인포인트 #1 프리뷰 기능 (방안 A: 정적 스키마 PNG).
    // 배포된 콘텐츠 스크립트 폴더의 {TYPE}_640.png를 팝업 창에 표시한다.
    // 이 버튼은 향후 방안 B(WebView2/three.js 라이브 렌더)의 동일 진입점이 된다.
    public partial class PaletteTab
    {
        private Button _btCatPreview;

        // InitializeSupportCatalog 말미에서 호출. Designer를 건드리지 않고 프로그래밍 방식으로 버튼을 단다.
        private void EnsureCatPreviewButton()
        {
            if (_btCatPreview != null) return;
            if (tabSupportCatalog == null) return;

            _btCatPreview = new Button
            {
                Name = "btCatPreview",
                Text = "Preview",
                Location = new Point(440, 11),
                Size = new Size(120, 23)
            };
            _btCatPreview.Click += btCatPreview_Click;
            tabSupportCatalog.Controls.Add(_btCatPreview);
            _btCatPreview.BringToFront(); // 런타임 Add는 z-order 뒤쪽 -> 덮는 컨테이너 뒤에 숨는 문제 해소
        }

        private void btCatPreview_Click(object sender, EventArgs e)
        {
            string template = cbCatType.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(template))
            {
                MessageBox.Show("먼저 Support Type을 선택하세요.", "Preview",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // B(WebView2/three.js) 우선. 초기화 실패 시 내부에서 A(PNG)로 폴백.
            try
            {
                ShowPreviewB(template);
            }
            catch (Exception ex)
            {
                Log("ShowPreviewB 진입 실패 → PNG 폴백: " + ex.Message);
                FallbackToPng(template, ex.Message);
            }
        }

        // 콘텐츠 스크립트 Support 폴더를 해석한다. .acat 경로와 어셈블리 경로를 앵커로 사용.
        private string ResolveSupportContentDir()
        {
            var cands = new List<string>();

            // 1) 현재 로드된 .acat 기준: ...\<Content Root>\Cpak Support\X.acat
            //    스크립트는 ...\<Content Root>\CPak Common\CustomScripts\Support 에 위치.
            try
            {
                string acat = _cat?.AcatPath;
                if (!string.IsNullOrEmpty(acat))
                {
                    string contentRoot = Path.GetDirectoryName(Path.GetDirectoryName(acat)); // up: Cpak Support -> Content Root
                    if (!string.IsNullOrEmpty(contentRoot))
                        cands.Add(Path.Combine(contentRoot, @"CPak Common\CustomScripts\Support"));
                }
            }
            catch (Exception ex) { Log("ResolveSupportContentDir(acat) skip: " + ex.Message); }

            // 2) 알려진 기본 콘텐츠 경로(개발/테스트 환경).
            cands.Add(@"D:\AutoCAD Plant 3D 2026 Content\CPak Common\CustomScripts\Support");

            // 3) 어셈블리 폴더 상위로 탐색하며 소스 트리 Support 폴더 폴백(개발 빌드).
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
                {
                    cands.Add(Path.Combine(dir, "Support"));
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch (Exception ex) { Log("ResolveSupportContentDir(asm) skip: " + ex.Message); }

            foreach (var c in cands)
            {
                try { if (Directory.Exists(c)) return c; }
                catch (Exception ex) { Log("ResolveSupportContentDir probe fail: " + ex.Message); }
            }
            return null;
        }

        // {TYPE}/{TYPE}_640.png (없으면 _200, _64 순). RS12 등 하위변형은 미해석(640 대표 이미지만).
        private string ResolveTypePng(string template)
        {
            string baseDir = ResolveSupportContentDir();
            if (baseDir == null) return null;

            string typeDir = Path.Combine(baseDir, template);
            foreach (var suffix in new[] { "_640.png", "_200.png", "_64.png" })
            {
                try
                {
                    string p = Path.Combine(typeDir, template + suffix);
                    if (File.Exists(p)) return p;
                }
                catch (Exception ex) { Log("ResolveTypePng probe fail: " + ex.Message); }
            }
            return null;
        }

        // 파일 잠금을 피하기 위해 스트림으로 읽어 메모리 복사본 비트맵을 만든다.
        private void ShowPreviewWindow(string template, string pngPath)
        {
            Image img;
            using (var fs = new FileStream(pngPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var tmp = Image.FromStream(fs))
            {
                img = new Bitmap(tmp);
            }

            var form = new Form
            {
                Text = $"Preview - {template}",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(680, 700),
                MinimizeBox = false,
                MaximizeBox = true
            };
            var pb = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = img
            };
            // 폼 종료 시 비트맵 해제(누수 방지).
            form.FormClosed += (s, e) =>
            {
                pb.Image = null;
                img.Dispose();
            };
            form.Controls.Add(pb);
            form.ShowDialog();
        }
    }
}
