"""Convert PSA PSGC publication workbook to LiteraHub JSON. pip install openpyxl"""
import argparse, collections, datetime, hashlib, json, pathlib, re
from openpyxl import load_workbook

def convert(source, destination, version, as_of):
    book=load_workbook(source,read_only=True,data_only=True)
    publication=next(r[1] for r in book['Metadata'].values if r[0]=='Publication date:')
    published=publication.date() if isinstance(publication,datetime.datetime) else datetime.datetime.strptime(str(publication).strip(),'%d %B %Y').date()
    if published.isoformat()!=as_of or f'{published.year}-Q{(published.month-1)//3+1}'!=version:
        raise ValueError('Version/as-of date does not match the workbook metadata')
    levels={'Reg','Prov','City','Mun','SubMun','Bgy'}
    rows={}
    for r in book['PSGC'].values:
        if r[4] not in levels: continue
        code=str(r[1]).zfill(10)
        if not re.fullmatch(r'\d{10}',code) or code in rows: raise ValueError(f'Invalid or duplicate code: {code}')
        rows[code]={'code':code,'name':str(r[2]).strip(),'level':r[4],'cityClass':r[6] or None}
    groups=collections.Counter(r['level'] for r in rows.values())
    national=next(r for r in book['National Summary'].values if r[2]=='PHILIPPINES')
    for level,count in zip(('Prov','City','Mun','Bgy'),national[3:7]):
        if groups[level]!=count: raise ValueError(f'{level}: {groups[level]} != {count}')
    region_count=sum(1 for r in book['National Summary'].values if str(r[0]) in rows and rows[str(r[0])]['level']=='Reg')
    if region_count!=groups['Reg']: raise ValueError('Region count mismatch')
    # Verify all four published geographic counts separately for each region.
    by_region=collections.Counter((code[:2], row['level']) for code,row in rows.items())
    for r in book['National Summary'].values:
        region=str(r[0])
        if region not in rows or rows[region]['level']!='Reg': continue
        for level,count in zip(('Prov','City','Mun','Bgy'),r[3:7]):
            if by_region[region[:2],level] != (count or 0):
                raise ValueError(f'{region} {level}: {by_region[region[:2],level]} != {count}')
    def parent(code,level):
        if code not in rows or rows[code]['level'] not in level: raise ValueError(f'Invalid parent {code}')
        return code
    result={'metadata':{'source':'Philippine Statistics Authority (PSA)','version':version,'asOf':as_of,'sourceFile':pathlib.Path(source).name,'sha256':hashlib.sha256(pathlib.Path(source).read_bytes()).hexdigest(),'counts':dict(groups)},'regions':[],'provinces':[],'citiesMunicipalities':[],'districts':[],'barangays':[]}
    for c,r in rows.items():
        n={'code':c,'name':r['name']}; k=r['level']
        # The 2026-Q3 workbook embeds a footnote marker in this locality name.
        # Keep the original text for traceability while displaying the geographic name.
        if c=='0431200000' and n['name']=='City of Lucenaa/':
            n['sourceName']=n['name']; n['name']='City of Lucena'
        if k=='Reg': result['regions'].append(n); continue
        n['regionCode']=parent(c[:2]+'00000000',{'Reg'})
        if k=='Prov': result['provinces'].append(n)
        elif k in ('City','Mun'):
            p=c[:5]+'00000'; n.update(level=k,cityClass=r['cityClass'],provinceCode=p if p in rows and rows[p]['level']=='Prov' and r['cityClass'] not in ('HUC','ICC') else None)
            result['citiesMunicipalities'].append(n)
        elif k=='SubMun':
            n['cityMunicipalityCode']=parent(c[:5]+'00000',{'City'});result['districts'].append(n)
        else:
            p=parent(c[:7]+'000',{'City','Mun','SubMun'});n['districtCode']=p if rows[p]['level']=='SubMun' else None
            n['cityMunicipalityCode']=parent(p[:5]+'00000',{'City'}) if n['districtCode'] else p
            result['barangays'].append(n)
    for k,v in result.items():
        if isinstance(v,list): v.sort(key=lambda r:r['code'])
    pathlib.Path(destination).write_text(json.dumps(result,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    print(groups, 'bytes:',pathlib.Path(destination).stat().st_size)
    book.close()

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source');p.add_argument('destination');p.add_argument('--version',required=True);p.add_argument('--as-of',required=True);a=p.parse_args();convert(a.source,a.destination,a.version,a.as_of)
