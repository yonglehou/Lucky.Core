/* 消息模板 */
define("dist/frameset/lettertemplate", [], '{{if count&&items.length>0}}\n<header class="panel-heading b-light bg-light js_msg_hdead">\n    <strong>你有 <span class="count js_msg_count">{{count}}</span>条未读消息</strong>\n</header>\n{{/if}}\n<div class="list-group list-group-alt  animated fadeInRigh js_msg_content_items" style="max-height:230px;overflow: hidden;overflow-y: auto">\n{{if items&&items.length>0}}\n {{each items as value i}}\n\n<a href="{{value.url}}" class="media list-group-item" target="{{value.target}}">\n    <span class="pull-left thumb-sm text-center">\n        {{if value.type==\'0\'}}\n        <i class="fa fa-bullhorn fa-2x text-success" title="系统"></i>\n        {{else if value.type==\'1\'}}\n        <i class="fa fa-envelope-o fa-2x text-success" title="消息"></i>\n        {{/if}}\n    </span>\n    <span class="media-body block m-b-none">\n        <span class="block">{{value.title}}</span>\n        <small class="text-muted">{{transMsgTime value.date}}</small>\n    </span>\n</a>\n{{/each}} {{else}}\n\n<div class="wrapper">您目前没有新消息</div>\n{{/if}}\n</div>');