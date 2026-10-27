"""Verify synthetic fixture integrity. This does not call the product extraction or RAG code."""
import argparse
import hashlib
import json
import logging
from pathlib import Path
import xml.etree.ElementTree as ET
from zipfile import ZipFile, BadZipFile

from pypdf import PdfReader
from pypdf.errors import PdfReadError

logging.getLogger('pypdf').setLevel(logging.CRITICAL)


def read_text(file):
    if file.suffix == '.pdf':
        return '\n'.join(page.extract_text() or '' for page in PdfReader(file).pages)
    if file.suffix == '.txt':
        return file.read_text(encoding='utf-8')
    with ZipFile(file) as archive:
        if archive.testzip():
            raise ValueError('Invalid ZIP member checksum')
        if file.suffix == '.docx':
            xmls = ['word/document.xml']
            tag = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t'
        else:
            xmls = sorted(name for name in archive.namelist()
                          if name.startswith('ppt/slides/slide') and name.endswith('.xml'))
            if not xmls:
                raise ValueError('No presentation slides')
            tag = '{http://schemas.openxmlformats.org/drawingml/2006/main}t'
        return '\n'.join(node.text or '' for xml in xmls
                         for node in ET.fromstring(archive.read(xml)).iter(tag))


def verify(root):
    manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
    fixtures = manifest['fixtures']
    paths = [item['path'] for item in fixtures]
    if len(paths) != len(set(paths)) or len(paths) != 19:
        raise ValueError('Expected 19 unique fixture paths')
    expected_paths = {
        *(f'valid/extraction.{ext}' for ext in ('pdf', 'docx', 'pptx', 'txt')),
        *(f'zero-byte/empty.{ext}' for ext in ('pdf', 'docx', 'pptx', 'txt')),
        *(f'empty-content/blank.{ext}' for ext in ('pdf', 'docx', 'pptx')),
        'empty-content/whitespace.txt',
        *(f'corrupt/truncated.{ext}' for ext in ('pdf', 'docx', 'pptx')),
        'corrupt/invalid-utf8.txt', 'rag/hs-a.txt', 'rag/hs-b.txt', 'rag/uni-a.txt',
    }
    if set(paths) != expected_paths:
        raise ValueError('Fixture coverage differs from the approved cases')
    for item in fixtures:
        file = (root / item['path']).resolve()
        if not file.is_relative_to(root.resolve()):
            raise ValueError('Manifest path escapes fixture directory')
        data = file.read_bytes()
        if len(data) != item['size_bytes'] or hashlib.sha256(data).hexdigest() != item['sha256']:
            raise ValueError(f'Integrity mismatch: {item["path"]}')
        kind = item['kind']
        if kind == 'zero_byte':
            if data:
                raise ValueError('Zero-byte fixture has content')
        elif kind == 'corrupt':
            try:
                read_text(file)
            except (PdfReadError, BadZipFile, UnicodeDecodeError):
                pass
            else:
                raise ValueError(f'Corrupt fixture unexpectedly parsed: {item["path"]}')
        else:
            text = read_text(file)
            if kind == 'empty_content' and text.strip():
                raise ValueError('Blank fixture has visible body text')
            for marker in item['expected_markers']:
                if marker not in text:
                    raise ValueError(f'Missing marker {marker!r} in {item["path"]}')
    corpus = json.loads((root / 'rag/corpus.json').read_text(encoding='utf-8'))['documents']
    queries = json.loads((root / 'rag/queries.json').read_text(encoding='utf-8'))
    by_course = {doc['fixture_course_id']: doc for doc in corpus}
    if len(by_course) != 3 or [doc['education_level'] for doc in corpus].count('HighSchool') != 2:
        raise ValueError('Expected two distinct high-school courses and one university course')
    for doc in corpus:
        text = (root / doc['source_file']).read_text(encoding='utf-8')
        if doc['sentinel'] not in text or any(other['sentinel'] in text for other in corpus if other != doc):
            raise ValueError('RAG sentinel contamination')
    for query in queries:
        if query['id'] == 'query-empty-course':
            if query['fixture_course_id'] in by_course or query['expected_count'] != 0:
                raise ValueError('Empty-course query has content')
            continue
        doc = by_course[query['fixture_course_id']]
        if query['required_sentinel'] != doc['sentinel'] or query['allowed_document_ids'] != [doc['fixture_document_id']]:
            raise ValueError('Query oracle differs from corpus')
        if set(query['forbidden_sentinels']) != {other['sentinel'] for other in corpus if other != doc}:
            raise ValueError('Incomplete cross-course negative expectations')
    return {'fixture_count': len(fixtures), 'integrity': 'PASS',
            'reference_parser_checks': 'PASS', 'rag_fixture_consistency': 'PASS',
            'product_smoke_tests': 'NOT_RUN'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parent)
    args = parser.parse_args()
    print(json.dumps(verify(args.root), indent=2))
