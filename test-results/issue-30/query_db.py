# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

import sqlite3, sys
db = r'C:\Users\Martin\AppData\Local\User Name\com.companyname.reporter\Data\reporter.db'
uri = 'file:' + db.replace('\\', '/') + '?mode=ro'
con = sqlite3.connect(uri, uri=True)
cur = con.cursor()
print('--- settings ---')
try:
    for row in cur.execute('SELECT * FROM settings'):
        print(row)
except Exception as e:
    print('settings err', e)
print('--- feeds ---')
try:
    cols = [d[1] for d in cur.execute('PRAGMA table_info(feeds)')]
    print('cols:', cols)
    for row in cur.execute('SELECT title, health_status FROM feeds'):
        print(row)
except Exception as e:
    print('feeds err', e)
print('--- categories ---')
try:
    for row in cur.execute('SELECT name FROM categories'):
        print(row)
except Exception as e:
    print('cat err', e)
print('--- saved items count ---')
try:
    print(cur.execute('SELECT COUNT(*) FROM items WHERE saved_for_later=1').fetchone())
except Exception as e:
    print('items err', e)
con.close()
