# Fansync Importer App
Pledge importer for https://fansync.moe/

## Documentation
All this application does is get supporter info from `https://api.fanbox.cc/legacy/manage/pledge/monthly?month={month}` and posts it to `https://fansync.moe/api/creator/{pixiv_id}/pledges?month={month}`.
It has a few extra features to make it *just work*, but that's about it.

If you're looking to implement something yourself, here's a barebones example of what the app does every hour.
```python
import os
import requests
from datetime import datetime

FANBOX_COOKIE = os.environ['FANBOX_COOKIE']
PIXIV_ID = os.environ['PIXIV_ID']
FANSYNC_TOKEN = os.environ['FANSYNC_TOKEN']

month = datetime.now().strftime('%Y-%m')


session = requests.Session()
session.headers.update({
    'Origin': 'https://www.fanbox.cc',
    'Referer': 'https://www.fanbox.cc',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36',
})
cookie = requests.cookies.create_cookie(name='FANBOXSESSID', value=FANBOX_COOKIE)
session.cookies.set_cookie(cookie)

pledges = session.get('https://api.fanbox.cc/legacy/manage/pledge/monthly?month={}'.format(month))
data = pledges.text

url = 'https://fansync.moe/api/creator/{}/pledges'.format(PIXIV_ID)
requests.post(url, params={'token': FANSYNC_TOKEN, 'month': month}, data=data)
```
