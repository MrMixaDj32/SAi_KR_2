import os
import re
import docx
from docx.shared import Pt, Mm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

def set_cell_margins(cell, top=100, bottom=100, left=150, right=150):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement('w:tcMar')
    for m, val in [('w:top', top), ('w:bottom', bottom), ('w:left', left), ('w:right', right)]:
        node = OxmlElement(m)
        node.set(qn('w:w'), str(val))
        node.set(qn('w:type'), 'dxa')
        tcMar.append(node)
    tcPr.append(tcMar)

def set_cell_shading(cell, color_hex):
    shading_elm = parse_xml(f'<w:shd {nsdecls("w")} w:fill="{color_hex}"/>')
    cell._tc.get_or_add_tcPr().append(shading_elm)

def set_cell_border(cell, **kwargs):
    """
    kwargs: top, bottom, left, right
    values: dict(sz=4, val='single', color='AAAAAA')
    """
    tcPr = cell._tc.get_or_add_tcPr()
    tcBorders = OxmlElement('w:tcBorders')
    for edge in ('top', 'left', 'bottom', 'right', 'insideH', 'insideV'):
        edge_data = kwargs.get(edge)
        if edge_data:
            tag = f'w:{edge}'
            element = OxmlElement(tag)
            element.set(qn('w:val'), edge_data.get('val', 'single'))
            element.set(qn('w:sz'), str(edge_data.get('sz', 4)))
            element.set(qn('w:space'), '0')
            element.set(qn('w:color'), edge_data.get('color', 'auto'))
            tcBorders.append(element)
    tcPr.append(tcBorders)

def add_styled_paragraph(doc, text="", style=None, align=WD_ALIGN_PARAGRAPH.JUSTIFY, 
                         first_line_indent=Mm(12.5), space_after=Pt(0), space_before=Pt(0), 
                         line_spacing=1.5, bold=False, italic=False, font_size=14, font_name="Times New Roman"):
    p = doc.add_paragraph()
    p.alignment = align
    p.paragraph_format.first_line_indent = first_line_indent
    p.paragraph_format.space_after = space_after
    p.paragraph_format.space_before = space_before
    p.paragraph_format.line_spacing = line_spacing

    if text:
        run = p.add_run(text)
        run.bold = bold
        run.italic = italic
        run.font.name = font_name
        run.font.size = Pt(font_size)
    return p

def main():
    md_path = r"c:\Users\artem\source\repos\SAi_KR_2\Пояснительная_записка_Курсовой_проект.md"
    docx_path = r"c:\Users\artem\source\repos\SAi_KR_2\Пояснительная_записка_Курсовой_проект.docx"
    
    with open(md_path, "r", encoding="utf-8") as f:
        md_content = f.read()

    doc = docx.Document()
    
    # Configure Section margins: Left=30mm, Right=15mm, Top=20mm, Bottom=20mm
    section = doc.sections[0]
    section.top_margin = Mm(20)
    section.bottom_margin = Mm(20)
    section.left_margin = Mm(30)
    section.right_margin = Mm(15)
    
    # Configure Normal Style
    style_normal = doc.styles['Normal']
    font = style_normal.font
    font.name = 'Times New Roman'
    font.size = Pt(14)
    font.color.rgb = RGBColor(0, 0, 0)
    style_normal.paragraph_format.line_spacing = 1.5
    style_normal.paragraph_format.space_after = Pt(0)
    style_normal.paragraph_format.space_before = Pt(0)
    style_normal.paragraph_format.first_line_indent = Mm(12.5)
    style_normal.paragraph_format.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY

    # Split md_content into sections separated by '---'
    raw_sections = md_content.split("\n---\n")
    print(f"Total markdown sections: {len(raw_sections)}")

    # 1. Title Page (raw_sections[0])
    p = add_styled_paragraph(doc, "Министерство науки и высшего образования Российской Федерации", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=12, bold=True)
    p = add_styled_paragraph(doc, "Федеральное государственное бюджетное образовательное учреждение высшего образования", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=12, bold=True)
    p = add_styled_paragraph(doc, "«КУБАНСКИЙ ГОСУДАРСТВЕННЫЙ ТЕХНОЛОГИЧЕСКИЙ УНИВЕРСИТЕТ»", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=13, bold=True)
    p = add_styled_paragraph(doc, "(ФГБОУ ВО «КубГТУ»)\n", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=12, bold=True)
    
    p = add_styled_paragraph(doc, "Факультет информационных технологий и кибербезопасности", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=12)
    p = add_styled_paragraph(doc, "Кафедра информационных систем и программирования\n", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=12)
    
    for _ in range(3):
        add_styled_paragraph(doc, "", first_line_indent=Mm(0))
        
    p = add_styled_paragraph(doc, "КУРСОВОЙ ПРОЕКТ", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=18, bold=True)
    p = add_styled_paragraph(doc, "по дисциплине: «Проектирование информационных систем (Системный анализ и принятие решений)»", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=14, bold=True)
    p = add_styled_paragraph(doc, "на тему: «Проектирование и разработка мультиагентной системы адаптивного управления светофорным регулированием транспортного коридора (на примере ул. Северной г. Краснодара)»\n", 
                             align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=14, italic=True)

    for _ in range(4):
        add_styled_paragraph(doc, "", first_line_indent=Mm(0))

    # Author info block
    p = add_styled_paragraph(doc, "Направление подготовки: 09.03.04 Программная инженерия", first_line_indent=Mm(70), font_size=13)
    p = add_styled_paragraph(doc, "Профиль: Проектирование и разработка ПО", first_line_indent=Mm(70), font_size=13)
    p = add_styled_paragraph(doc, "Выполнил студент группы 23-КБ-ПРЗ: Лыков А.В.", first_line_indent=Mm(70), font_size=13)
    p = add_styled_paragraph(doc, "Руководитель проекта: проф. Зайков В.П.", first_line_indent=Mm(70), font_size=13)
    p = add_styled_paragraph(doc, "Члены комиссии: доц. Шумков Е.А.", first_line_indent=Mm(70), font_size=13)
    p = add_styled_paragraph(doc, "                              ст. преп. Кушнир Н.В.", first_line_indent=Mm(70), font_size=13)

    for _ in range(4):
        add_styled_paragraph(doc, "", first_line_indent=Mm(0))

    p = add_styled_paragraph(doc, "Краснодар\n2026", align=WD_ALIGN_PARAGRAPH.CENTER, first_line_indent=Mm(0), font_size=13, bold=True)
    doc.add_page_break()

    # Process remaining sections
    for sec_idx, sec_text in enumerate(raw_sections[1:], start=1):
        lines = sec_text.strip().split("\n")
        in_code_block = False
        code_lines = []
        table_lines = []
        in_table = False

        for line in lines:
            line_str = line.strip()
            
            # Code block handling
            if line_str.startswith("```"):
                if in_code_block:
                    # Flush code block
                    code_text = "\n".join(code_lines)
                    table = doc.add_table(rows=1, cols=1)
                    table.alignment = WD_TABLE_ALIGNMENT.CENTER
                    cell = table.cell(0, 0)
                    cell.width = Mm(165)
                    set_cell_shading(cell, "F2F2F2")
                    set_cell_border(cell, top={'val': 'single', 'sz': 4, 'color': 'CCCCCC'},
                                          bottom={'val': 'single', 'sz': 4, 'color': 'CCCCCC'},
                                          left={'val': 'single', 'sz': 12, 'color': '888888'},
                                          right={'val': 'single', 'sz': 4, 'color': 'CCCCCC'})
                    p = cell.paragraphs[0]
                    p.paragraph_format.first_line_indent = Mm(0)
                    p.paragraph_format.line_spacing = 1.0
                    p.paragraph_format.space_before = Pt(4)
                    p.paragraph_format.space_after = Pt(4)
                    run = p.add_run(code_text)
                    run.font.name = "Consolas"
                    run.font.size = Pt(9.5)
                    code_lines = []
                    in_code_block = False
                else:
                    in_code_block = True
                    code_lines = []
                continue

            if in_code_block:
                code_lines.append(line)
                continue

            # Table handling
            if line_str.startswith("|") and line_str.endswith("|"):
                in_table = True
                table_lines.append(line_str)
                continue
            elif in_table:
                # Flush table
                process_markdown_table(doc, table_lines)
                table_lines = []
                in_table = False

            if not line_str:
                continue

            # Headers
            if line_str.startswith("# "):
                h_text = line_str[2:].strip()
                p = add_styled_paragraph(doc, h_text, align=WD_ALIGN_PARAGRAPH.CENTER, 
                                         first_line_indent=Mm(0), font_size=16, bold=True, 
                                         space_before=Pt(12), space_after=Pt(8))
            elif line_str.startswith("## "):
                h_text = line_str[3:].strip()
                p = add_styled_paragraph(doc, h_text, align=WD_ALIGN_PARAGRAPH.LEFT, 
                                         first_line_indent=Mm(12.5), font_size=14, bold=True, 
                                         space_before=Pt(12), space_after=Pt(6))
            elif line_str.startswith("### "):
                h_text = line_str[4:].strip()
                p = add_styled_paragraph(doc, h_text, align=WD_ALIGN_PARAGRAPH.LEFT, 
                                         first_line_indent=Mm(12.5), font_size=14, bold=True, italic=True,
                                         space_before=Pt(8), space_after=Pt(4))
            elif line_str.startswith("#### "):
                h_text = line_str[5:].strip()
                p = add_styled_paragraph(doc, h_text, align=WD_ALIGN_PARAGRAPH.LEFT, 
                                         first_line_indent=Mm(12.5), font_size=13, bold=True,
                                         space_before=Pt(6), space_after=Pt(2))
            elif line_str.startswith("- ") or line_str.startswith("* "):
                bullet_text = line_str[2:].strip()
                p = doc.add_paragraph()
                p.paragraph_format.left_indent = Mm(17.5)
                p.paragraph_format.first_line_indent = Mm(-5)
                p.paragraph_format.line_spacing = 1.5
                p.paragraph_format.space_after = Pt(2)
                p.paragraph_format.space_before = Pt(0)
                p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
                run_bullet = p.add_run("•  ")
                run_bullet.font.name = "Times New Roman"
                run_bullet.font.size = Pt(14)
                format_inline_markdown(p, bullet_text)
            elif re.match(r"^\d+\.\s", line_str):
                num_match = re.match(r"^(\d+\.)\s*(.*)", line_str)
                num_prefix = num_match.group(1) + " "
                rest_text = num_match.group(2)
                p = doc.add_paragraph()
                p.paragraph_format.left_indent = Mm(17.5)
                p.paragraph_format.first_line_indent = Mm(-5)
                p.paragraph_format.line_spacing = 1.5
                p.paragraph_format.space_after = Pt(2)
                p.paragraph_format.space_before = Pt(0)
                p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
                run_num = p.add_run(num_prefix + " ")
                run_num.font.name = "Times New Roman"
                run_num.font.size = Pt(14)
                format_inline_markdown(p, rest_text)
            else:
                # Regular paragraph
                p = doc.add_paragraph()
                p.paragraph_format.first_line_indent = Mm(12.5)
                p.paragraph_format.line_spacing = 1.5
                p.paragraph_format.space_after = Pt(0)
                p.paragraph_format.space_before = Pt(0)
                p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
                format_inline_markdown(p, line_str)

        if in_table and table_lines:
            process_markdown_table(doc, table_lines)

        # Page break after major sections except the very last one
        if sec_idx < len(raw_sections) - 1:
            doc.add_page_break()

    doc.save(docx_path)
    print(f"Successfully generated docx at: {docx_path}")

def format_inline_markdown(paragraph, text):
    """Parses bold, italic, and inline code formatting"""
    # Replace markdown math notations for cleaner reading
    text = text.replace("$$", "").replace("$", "")
    
    # Tokenize by bold (**), italic (* or _), code (`)
    tokens = re.split(r'(\*\*.*?\*\*|\*.*?\*|`.*?`)', text)
    for token in tokens:
        if not token:
            continue
        if token.startswith("**") and token.endswith("**") and len(token) >= 4:
            run = paragraph.add_run(token[2:-2])
            run.bold = True
        elif token.startswith("*") and token.endswith("*") and len(token) >= 2:
            run = paragraph.add_run(token[1:-1])
            run.italic = True
        elif token.startswith("`") and token.endswith("`") and len(token) >= 2:
            run = paragraph.add_run(token[1:-1])
            run.font.name = "Consolas"
            run.font.size = Pt(11)
            run.font.color.rgb = RGBColor(40, 40, 120)
        else:
            run = paragraph.add_run(token)
        
        if not (token.startswith("`") and token.endswith("`")):
            run.font.name = "Times New Roman"
            run.font.size = Pt(14)

def process_markdown_table(doc, table_lines):
    rows = []
    for line in table_lines:
        cells = [c.strip() for c in line.strip('|').split('|')]
        # check if it's separator row
        if cells and all(set(c).issubset({'-', ':', ' '}) for c in cells):
            continue
        rows.append(cells)
    
    if not rows:
        return

    num_cols = max(len(r) for r in rows)
    table = doc.add_table(rows=len(rows), cols=num_cols)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    
    for r_idx, row_cells in enumerate(rows):
        is_header = (r_idx == 0)
        for c_idx in range(num_cols):
            cell_text = row_cells[c_idx] if c_idx < len(row_cells) else ""
            cell = table.cell(r_idx, c_idx)
            set_cell_margins(cell, top=120, bottom=120, left=150, right=150)
            set_cell_border(cell, top={'val': 'single', 'sz': 4, 'color': '888888'},
                                  bottom={'val': 'single', 'sz': 4, 'color': '888888'},
                                  left={'val': 'single', 'sz': 4, 'color': '888888'},
                                  right={'val': 'single', 'sz': 4, 'color': '888888'})
            if is_header:
                set_cell_shading(cell, "EAEAEA")
            
            p = cell.paragraphs[0]
            p.paragraph_format.first_line_indent = Mm(0)
            p.paragraph_format.line_spacing = 1.15
            p.paragraph_format.space_before = Pt(2)
            p.paragraph_format.space_after = Pt(2)
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER if is_header else WD_ALIGN_PARAGRAPH.LEFT
            
            format_inline_markdown(p, cell_text)
            for run in p.runs:
                run.font.name = "Times New Roman"
                run.font.size = Pt(11)
                if is_header:
                    run.bold = True

if __name__ == "__main__":
    main()
