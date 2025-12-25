import os

path = r'c:\WORK\SRC\Railml_editor\RailmlEditor\MainWindow.xaml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Remove Preview handler
content = content.replace('PreviewMouseLeftButtonDown="Thumb_PreviewMouseLeftButtonDown" ', '')

# Ensure Width/Height on Thumbs
import re
# Find Thumbs and add dimensions if missing
def thumb_fix(match):
    text = match.group(0)
    if 'Width=' not in text:
        text = text.replace('<Thumb ', '<Thumb Width="12" Height="12" ')
    return text

content = re.sub(r'<Thumb[^>]+>', thumb_fix, content)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)
