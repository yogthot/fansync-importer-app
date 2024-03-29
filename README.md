# Fansync Importer App
Pledge importer for https://fansync.moe/

## Documentation
All this application does is get plan and supporter info from fanbox apis and then sends it to the corresponding fansync api.
It has a few extra features to make it *just work*, but that's about it.

If you're looking to implement something yourself, here's a barebones example of what the app does every hour.
```python
import os
import requests
from datetime import datetime

FANBOX_COOKIE = os.environ['FANBOX_COOKIE']
PIXIV_ID = os.environ['PIXIV_ID']
FANSYNC_TOKEN = os.environ['FANSYNC_TOKEN']


session = requests.Session()
session.headers.update({
    'Origin': 'https://www.fanbox.cc',
    'Referer': 'https://www.fanbox.cc',
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69 Safari/537.36',
})
cookie = requests.cookies.create_cookie(name='FANBOXSESSID', value=FANBOX_COOKIE)
session.cookies.set_cookie(cookie)

plans = session.get('https://api.fanbox.cc/plan.listCreator?userId={}'.format(PIXIV_ID))
planData = plans.text

supporters = session.get('https://api.fanbox.cc/relationship.listFans?status=supporter')
supporterData = supporters.text

params = {'token': FANSYNC_TOKEN}
# don't use the same session here
requests.post('https://fansync.moe/api/creator/{}/plans'.format(PIXIV_ID), params=params, data=planData)
requests.post('https://fansync.moe/api/creator/{}/supporters'.format(PIXIV_ID), params=params, data=supporterData)
```
